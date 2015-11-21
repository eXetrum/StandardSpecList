using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using Excel = Microsoft.Office.Interop.Excel;
using HtmlAgilityPack;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;


namespace StandardSpecList
{
    public partial class mainForm : Form
    {
        // Рефер. Используем только для главной страницы
        static string mainPageReffer = "http://google.com";
        // Целевой URI
        static string targetUrl = "http://protect.gost.ru";
        // Главная страница
        static string initPageUrl = targetUrl + "/default.aspx";
        // Страница поиска
        static string searchUrl = initPageUrl + "?search=";
        // Страница загрузки изображений
        static string imgLoaderPage = targetUrl + "/image.ashx";
        // Прикинемся "огнелисом"
        static string userAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:44.0) Gecko/20100101 Firefox/44.0";
        // Аксецптим такой набор
        static string accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        // Имя файла для обработки по умолчанию
        string filename = "Список гостов.xlsx";
        // Максимальное время ожидания
        static int maxTimeout = 50000;
        // Если используем прокси выставить маркер в true
        static bool allowProxy = false;
        static WebProxy proxy = new WebProxy("127.0.0.1", 8888);
        // Список событий для файлов (маркеры завершения)
        ConcurrentDictionary<string, ManualResetEvent> events = new ConcurrentDictionary<string, ManualResetEvent>();
        TextWriter tw = null;

        static bool workDone = true;

        public mainForm()
        {
            InitializeComponent();
            // Здаем файл лог
            tw = new StreamWriter(File.Open("log.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read));
        }

        // Объект синхронизации
        private static readonly object _syncObject = new object();
        // Метод потокобезопасного сброса в файл 
        public static void Log(string logMessage, TextWriter w)
        {
            // only one thread can own this lock, so other threads
            // entering this method will wait here until lock is
            // available.
            lock (_syncObject)
            {
                w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                w.WriteLine("{0}", logMessage);
                w.WriteLine("-------------------------------");
                // Update the underlying file.
                w.Flush();
            }
        }
        // Метод обработки файла
        private void ProcessSpec(object o)
        {
            ListViewItem lvi = o as ListViewItem;
            string specName = lvi.Text;
            StringBuilder specLogs = new StringBuilder();
            specLogs.Append("=======[" + specName + "]=======" + Environment.NewLine);
            bool errors = false;
            try
            {
                /////////////////////////////////////////////////////////////////////////////////
                /// Запрос на главную страницу
                /////////////////////////////////////////////////////////////////////////////////
                var initPageRequrest = (HttpWebRequest)HttpWebRequest.Create(initPageUrl);
                initPageRequrest.Proxy = null;
                if (allowProxy)
                    initPageRequrest.Proxy = proxy;
                initPageRequrest.UserAgent = userAgent;
                initPageRequrest.Accept = accept;
                initPageRequrest.Referer = mainPageReffer;
                initPageRequrest.AllowAutoRedirect = false;
                initPageRequrest.KeepAlive = false;
                initPageRequrest.Timeout = maxTimeout;
                initPageRequrest.Headers.Add("Accept-Language", "ru");
                // Получаем ответ
                var initPageResponse = (HttpWebResponse)initPageRequrest.GetResponse();
                // Достаем куки
                string sCookies =
                    (String.IsNullOrEmpty(initPageResponse.Headers["Set-Cookie"])
                    ? "" : initPageResponse.Headers["Set-Cookie"]);
                // Освобождаем ресурсы ответа
                initPageResponse.Close();
                // Заносим в лог данные
                specLogs.Append("[GET start page OK]" + Environment.NewLine);
                specLogs.Append("[cookie]:" + sCookies + Environment.NewLine);
                /////////////////////////////////////////////////////////////////////////////////
                /// Запрос на страницу поиска
                /////////////////////////////////////////////////////////////////////////////////
                var searchPageRequest = (HttpWebRequest)HttpWebRequest.Create(searchUrl + specName);
                searchPageRequest.Proxy = null;
                if (allowProxy)
                    searchPageRequest.Proxy = proxy;
                searchPageRequest.UserAgent = userAgent;
                searchPageRequest.Accept = accept;
                // Укажем что перешли с главной страницы
                searchPageRequest.Referer = initPageUrl;
                searchPageRequest.AllowAutoRedirect = false;
                searchPageRequest.KeepAlive = false;
                searchPageRequest.Timeout = maxTimeout;
                searchPageRequest.Headers.Add("Accept-Language", "ru");
                // Добавляем куки полученные на предыдущем шаге
                searchPageRequest.Headers.Add(HttpRequestHeader.Cookie, sCookies);
                // Сюда запишем html текст страницы
                string searchPageHTML = string.Empty;
                // Сюда запишем рефера для последующих переходов
                string nextRefer = string.Empty;
                // Получаем ответ
                var searchPageResponse = (HttpWebResponse)searchPageRequest.GetResponse();
                using (var searchResponseStream = searchPageResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(searchResponseStream, Encoding.GetEncoding(searchPageResponse.CharacterSet)))
                    {
                        // Запоминаем ответ URI в качестве рефера для следующих шагов
                        nextRefer = searchPageResponse.ResponseUri.ToString();
                        // Запомнили html код страницы
                        searchPageHTML = sr.ReadToEnd();
                        // Логируем процесс
                        specLogs.Append("[GET search page OK]" + Environment.NewLine);
                        specLogs.Append("[search page response uri]:" + nextRefer + Environment.NewLine);
                    }
                }
                // Закрыли соединение
                searchPageResponse.Close();
                // Парсим полученный код страницы
                HtmlAgilityPack.HtmlDocument searchPageDoc = new HtmlAgilityPack.HtmlDocument();
                searchPageDoc.LoadHtml(searchPageHTML);
                // Ищем таблицу с результатами поиска (ссылки)
                HtmlNodeCollection NoAltElements = searchPageDoc.DocumentNode.SelectNodes("//table[@class='typetable']//td[@class='tx12']//a");
                // Логируем процесс
                specLogs.Append("[start parse search link]" + Environment.NewLine);
                // Проверка на наличие найденных узлов. Нас интересует чтобы узлы были и чтобы узлов было ровно один
                string linkToTitleDoc = string.Empty;
                if (NoAltElements != null && NoAltElements.Count == 1)
                {
                    // Получаем ссылку на страницу документа
                    linkToTitleDoc = NoAltElements[0].Attributes["href"].Value;
                    // Логируем
                    specLogs.Append("[parse search link]: " + linkToTitleDoc + Environment.NewLine);
                }
                    // Если ссылок нет или ссыло больше одной - кидаем исключение
                else
                {
                    // search link not found or multiple entry's
                    throw new Exception("search link not found or multiple entry's" + Environment.NewLine);
                }
                /////////////////////////////////////////////////////////////////////////////////
                /// Запрос на титульную страницу описания госта
                /////////////////////////////////////////////////////////////////////////////////
                var specTitlePageReq = (HttpWebRequest)HttpWebRequest.Create(targetUrl + "/" + linkToTitleDoc);
                specTitlePageReq.Proxy = null;
                if (allowProxy)
                    specTitlePageReq.Proxy = proxy;
                specTitlePageReq.UserAgent = userAgent;
                specTitlePageReq.Accept = accept;
                specTitlePageReq.Referer = HttpUtility.UrlEncode(nextRefer);
                specTitlePageReq.AllowAutoRedirect = false;
                specTitlePageReq.KeepAlive = false;
                specTitlePageReq.Timeout = maxTimeout;
                specTitlePageReq.Headers.Add("Accept-Language", "ru");
                specTitlePageReq.Headers.Add(HttpRequestHeader.Cookie, sCookies);
                // HTML код титульной страницы выбранного ГОСТ'а
                string specTitlePageHTML = string.Empty;
                nextRefer = string.Empty;
                // Получаем ответ
                var specTitlePageResponse = (HttpWebResponse)specTitlePageReq.GetResponse();
                using (var specTitlePageStream = specTitlePageResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(specTitlePageStream, Encoding.GetEncoding(specTitlePageResponse.CharacterSet)))
                    {
                        // Запоминаем рефер
                        nextRefer = specTitlePageResponse.ResponseUri.ToString();
                        // Получаем код страницы
                        specTitlePageHTML = sr.ReadToEnd();
                        // Логируем
                        specLogs.Append("[spec page title OK]" + Environment.NewLine);
                        specLogs.Append("[response uri]: " + nextRefer + Environment.NewLine);
                    }
                }
                specTitlePageResponse.Close();
                // Парсим полученный документ
                HtmlAgilityPack.HtmlDocument specPageDoc = new HtmlAgilityPack.HtmlDocument();
                specPageDoc.LoadHtml(specTitlePageHTML);
                // Ищем таблицу с результатами поиска(ссылки)
                specLogs.Append("[parse spec title page link]" + Environment.NewLine);
                NoAltElements = specPageDoc.DocumentNode.SelectNodes("//td[@class='document']//td[@class='download']//a");
                string linkToFullDoc = string.Empty;
                if (NoAltElements != null && NoAltElements.Count == 1)
                {
                    // Получаем ссылку на полное описание документа
                    linkToFullDoc = NoAltElements[0].Attributes["href"].Value;
                    // Логируем
                    specLogs.Append("[page link]:" + linkToFullDoc + Environment.NewLine);
                }
                // link to full spec not found
                else
                {
                    throw new Exception("link to full spec page not found" + Environment.NewLine);
                }
                /////////////////////////////////////////////////////////////////////////////////
                /// Запрос на страницу полного описания госта
                /////////////////////////////////////////////////////////////////////////////////
                var specFullPageReq = (HttpWebRequest)HttpWebRequest.Create(targetUrl + "/" + linkToFullDoc);
                specFullPageReq.Proxy = null;
                if (allowProxy)
                    specFullPageReq.Proxy = proxy;
                specFullPageReq.UserAgent = userAgent;
                specFullPageReq.Accept = accept;
                specFullPageReq.Referer = HttpUtility.UrlEncode(nextRefer);
                specFullPageReq.AllowAutoRedirect = false;
                specFullPageReq.KeepAlive = false;
                specFullPageReq.Timeout = maxTimeout;
                specFullPageReq.Headers.Add("Accept-Language", "ru");
                specFullPageReq.Headers.Add(HttpRequestHeader.Cookie, sCookies);
                // Сюда положим код страницы
                string specFullPageHTML = string.Empty;
                nextRefer = string.Empty;
                // Получаем ответ
                var specFullPageResponse = (HttpWebResponse)specFullPageReq.GetResponse();
                using (var specFullPagePesponse = specFullPageResponse.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(specFullPagePesponse, Encoding.GetEncoding(specFullPageResponse.CharacterSet)))
                    {
                        // Запоминаем рефер
                        nextRefer = specFullPageResponse.ResponseUri.ToString();
                        // Получаем html код страницы
                        specFullPageHTML = sr.ReadToEnd();
                        // Логируем
                        specLogs.Append("[spec full page OK]" + Environment.NewLine);
                        specLogs.Append("[response uri]: " + nextRefer + Environment.NewLine);
                    }
                }
                specFullPageResponse.Close();
                // Парсим документ
                HtmlAgilityPack.HtmlDocument specFullPageDoc = new HtmlAgilityPack.HtmlDocument();
                specPageDoc.LoadHtml(specFullPageHTML);
                // Ищем ссылки страниц
                NoAltElements = specPageDoc.DocumentNode.SelectNodes("//td[@class='document']//td[@class='download']//a");
                // Список ссылок на страницы
                List<string> pages = new List<string>();
                // Если ссылки есть
                if (NoAltElements != null)
                {
                    foreach (var HR in NoAltElements)
                    {
                        if (HR.InnerText.Equals((pages.Count + 1).ToString()))
                            pages.Add(HR.Attributes["href"].Value);
                    }
                    // Собираем ссылки для получения изображений
                    List<string> images = new List<string>();
                    for (int i = 0; i < pages.Count; ++i)
                    {
                        int index = pages[i].IndexOf("pageK=");
                        if (index == -1) continue;

                        string pageID = pages[i].Substring(index + 6);
                        images.Add(pageID);
                    }
                    // Логируем
                    specLogs.Append("[parse image links ]: " + Environment.NewLine + string.Join(Environment.NewLine, images.ToArray()) + Environment.NewLine);
                    // Создаем директорию с названием госта
                    DirectoryInfo di = System.IO.Directory.CreateDirectory(specName);
                    // Скачиваем изображения
                    for (int img = 0; img < images.Count; ++img)
                    {
                        var imgReq = (HttpWebRequest)HttpWebRequest.Create(imgLoaderPage + "?page=" + images[img]);
                        imgReq.Proxy = null;
                        if (allowProxy)
                            imgReq.Proxy = proxy;
                        imgReq.UserAgent = userAgent;
                        imgReq.Accept = accept;
                        imgReq.Referer = HttpUtility.UrlEncode(nextRefer);
                        imgReq.AllowAutoRedirect = false;
                        imgReq.Timeout = maxTimeout;
                        imgReq.Headers.Add("Accept-Language", "ru");
                        imgReq.Headers.Add(HttpRequestHeader.Cookie, sCookies);
                        // Получаем ответ сервера (текущее изображение)
                        var imgResp = (HttpWebResponse)imgReq.GetResponse();
                        long contentLen = imgResp.ContentLength;
                        using (var binaryReader = new BinaryReader(imgResp.GetResponseStream()))
                        {
                            byte[] imgBytes = null;
                            using (var memStream = new MemoryStream())
                            {
                                byte[] buff = new byte[1024];
                                int read = 0;

                                while ((read = binaryReader.Read(buff, 0, buff.Length)) > 0)
                                {
                                    memStream.Write(buff, 0, read);
                                }
                                imgBytes = memStream.ToArray();
                            }
                            // write
                            using (BinaryWriter bw = new BinaryWriter(new StreamWriter(di.FullName + "/" + "page #" + img + ".jpeg").BaseStream))
                            {
                                bw.Write(imgBytes);
                            }
                            specLogs.Append("[img " + images[img] + " read OK]" + Environment.NewLine);

                        }
                        imgResp.Close();
                    }

                }
                // pages not found
                else
                {
                    throw new Exception("pages not found" + Environment.NewLine);
                }                         
            }
            catch (Exception ex)
            {
                errors = true;
                specLogs.Append("[Произошла ошибка]: " + ex.Message + Environment.NewLine);
                specLogs.Append("[Стек]: " + ex.StackTrace + Environment.NewLine);
            }
                // По окончанию работы метода
            finally
            {
                // Если были ошибки 
                if (errors)
                {
                    // Добавляем в лог
                    Log(specLogs.ToString(), tw);
                    this.Invoke(new Action(() => { lvi.SubItems[1].Text = "Ошибка"; }));
                }
                else
                {
                    this.Invoke(new Action(() => { lvi.SubItems[1].Text = "Готово"; } ));
                }
                // Маркируем как завершенное задание
                events[specName].Set();
                // Двигаем прогрессбар
                this.Invoke(new Action(() => { workProgressBar.PerformStep(); }));
            }
        }

        // Метод отслеживающий выполнение заданий
        private void Watcher()
        {
            // Маркируем завершение
            workDone = false;
            int doneTask = 0;
            int totalTask = events.Count;
            // Включаем показ прогрессбара
            this.Invoke(new Action( () =>
            {
                workProgressBar.Minimum = 1;
                workProgressBar.Maximum = totalTask * 10;
                workProgressBar.Step = 10;
                workProgressBar.Value = 1;
                workProgressBar.Visible = true;
            }));
            try
            {
                // Пока количество выполненных задач не равно полному количество работ крутим цикл и проверяем сколько уже выполнено
                while (doneTask < totalTask)
                {
                    // Обнуляем счетчик выполненных работ
                    doneTask = 0;
                    // Проверяем все задачи
                    foreach (var task in events)
                    {
                        // Увеличим счетчик если текущее задание готово
                        doneTask += (!task.Value.WaitOne(100) ? 0 : 1);
                    }
                }
            }
            // Если произошла ошибка
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
                // Метод завершает работу
            finally
            {
                // Выключаем показ прогресс бара
                this.Invoke(new Action(() =>
                {
                    workProgressBar.Visible = false;
                }));
                // Включаем кнопки
                btnLoad.Enabled = true;
                btnProcess.Enabled = true;
                // Маркируем завершение
                workDone = true;
            }
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            // Запуск процесса обработки задач
            // Выключаем кнопки загрузки и повторного запуска
            btnLoad.Enabled = false;
            btnProcess.Enabled = false;
            workDone = false;
            // Очищаем список задач
            events.Clear();
            try
            {
                // Создаем поток "смотритель" (следит за ходом выполнения работ)
                Thread worker = new Thread(Watcher);
                // Для каждого файл из списка запускаем обработку
                for (int i = 0; i < specListView.Items.Count; ++i)
                {
                    // Получили элемент списка
                    ListViewItem lvi = specListView.Items[i];
                    // Получаем имя файла
                    string curSpec = lvi.Text;
                    // Меняем статус файла на "в процессе обработки"
                    lvi.SubItems[1].Text = "обработка...";
                    // Создаем поток для обработки нового файла; укажем что поток фоновый (чтобы потоки завершались при закрытии приложения)
                    Thread th = new Thread(ProcessSpec) { IsBackground = true };
                    // Создаем событие-задачу для обрабатываемого файла
                    events.TryAdd(curSpec, new ManualResetEvent(false));
                    // Запускаем поток
                    th.Start(specListView.Items[i]);
                }
                // Запускаем поток "прораба"
                worker.Start();
            }
                // Отлавливаем ошибки
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        // Метод загрузки данных из файла
        private void btnLoad_Click(object sender, EventArgs e)
        {
            // Создаем и отображаем диалог открытия файла
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Укажите файл - список";
            ofd.Filter = "Office Excel 2007 (*.xlsx)|*.xlsx|Old Office Excel (*.xls) | *.xls";
            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            // Выключаем кнопку загрузки файла
            btnLoad.Enabled = false;
            // СОМ объекты для работы с Excel файлом
            Excel.Application xlApp = null;
            Excel.Workbooks xlWorkBooks = null;
            Excel.Workbook xlWorkBook = null;
            Excel.Sheets xlSheets = null;
            Excel.Worksheet xlWorkSheet = null;
            Excel.Range range = null;
            List<Excel.Range> cells = new List<Excel.Range>();
            try
            {
                // Запоминаем имя файла
                filename = ofd.FileName;
                // Очищаем список
                specListView.Items.Clear();
                // Создаем объект для работы с Excel приложением
                xlApp = new Excel.Application() { Visible = false };
                xlWorkBooks = xlApp.Workbooks;
                // Открываем заданный файл
                xlWorkBook = xlWorkBooks.Open(filename);
                xlSheets = xlWorkBook.Worksheets;
                xlWorkSheet = xlSheets.get_Item(1);
                // Получаем список используемых ячейек
                range = xlWorkSheet.UsedRange;
                // Получаем количество строк
                Excel.Range rowRange = range.Rows;
                int rows = rowRange.Count;
                // Обрабатываем ячейки
                for (int i = 1; i <= rows; ++i)
                {
                    // Получаем столбцы
                    Excel.Range colRange = rowRange.Columns;
                    int columns = colRange.Count;
                    for (int j = 1; j <= columns; ++j)
                    {
                        // Получили доступ к ячейке
                        Excel.Range cell = colRange.Cells[i, j];
                        // Получили текст ячейки
                        string text = cell.Value2 as string;
                        // Освобождаем ресурсы выделенные для связи с ячейкой
                        Marshal.ReleaseComObject(cell);
                        // Форсим использование Garbage Collector
                        cell = null;
                        GC.GetTotalMemory(true);
                        GC.Collect();
                        GC.WaitForPendingFinalizers();  
                        // Маркер присутствия в списке
                        bool already = false;
                        // Поиск имени файла в списке
                        for (int lv = 0; lv < specListView.Items.Count && !already; ++lv)
                            if (specListView.Items[lv].Text.Equals(text)) already = true;
                        // Если имени файла еще нет в списке
                        if(!already)
                        {
                            // Создаем новый элемент
                            ListViewItem lvi = new ListViewItem(text);
                            // Задаем статус "готов к обаботке" или "ожидает обработку"
                            lvi.SubItems.Add("Ожидает");
                            // Добавляем в список
                            specListView.Items.Add(lvi);
                        }
                    }
                    // Освобождаем СОМ объект столбцов
                    Marshal.ReleaseComObject(colRange);
                }
                // Освобождаем СОМ объект строк
                Marshal.ReleaseComObject(rowRange);
                // Заносим данные о количестве файлов
                lblSpecListCount.Text = "Общее количество: " + specListView.Items.Count.ToString();
                lblFileName.ForeColor = Color.Brown;
                lblFileName.Text = ofd.SafeFileName;
            }
                // Отлавливаем ошибки
            catch (Exception ex) { MessageBox.Show(ex.Message); }
                // По окончанию работы метода
            finally
            {
                // Освобождаем все COM объекты
                foreach (var cell in cells)
                    Marshal.FinalReleaseComObject(cell);

                if (range != null) Marshal.FinalReleaseComObject(range);
                if (xlWorkSheet != null) Marshal.FinalReleaseComObject(xlWorkSheet);
                if (xlSheets != null)
                {
                    Marshal.FinalReleaseComObject(xlSheets);
                }
                if (xlWorkBook != null)
                {
                    xlWorkBook.Close(false, Type.Missing, Type.Missing);
                    Marshal.FinalReleaseComObject(xlWorkBook);
                }
                if (xlWorkBooks != null)
                {
                    xlWorkBooks.Close();
                    Marshal.FinalReleaseComObject(xlWorkBooks);
                }
                if (xlApp != null)
                {
                    xlApp.Application.Quit();
                    xlApp.Quit();
                    Marshal.FinalReleaseComObject(xlApp);
                }
                range = null;
                xlWorkSheet = null;
                xlSheets = null;
                xlWorkBook = null;
                xlWorkBooks = null;
                xlApp = null;

                GC.GetTotalMemory(false);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.GetTotalMemory(true);

                btnLoad.Enabled = true;
                if (specListView.Items.Count > 0) btnProcess.Enabled = true;
            }
        }
        // Запретим изменение ширины столбцов
        private void specListView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = specListView.Columns[e.ColumnIndex].Width;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            if (!workDone)
            {
                MessageBox.Show("Работа в процессе...");
                return;
            }
            this.Close();
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!workDone)
            {
                MessageBox.Show("Работа в процессе...");
                return;
            }
            if(tw != null)
                tw.Close();
        }

    }
}
