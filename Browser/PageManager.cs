using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;

namespace Browser
{
    public partial class PageManager : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attr,
            int[] attrValue,
            int attrSize
        );

        protected override void OnHandleCreated(EventArgs e)
        {
            if (DwmSetWindowAttribute(Handle, 19, new[] { 1 }, 4) != 0)
                DwmSetWindowAttribute(Handle, 20, new[] { 1 }, 4);
        }

        public class Tab
        {
            public string Id { get; set; }
            public string Source { get; set; }
            public WebView2 WebView { get; set; }
        }

        public class Bookmark
        {
            public string Title { get; set; }
            public string Url { get; set; }
        }

        public class Settings
        {
            public string DefaultSearchEngine { get; set; }
        }

        private List<Bookmark> Bookmarks = new List<Bookmark>();

        private Settings MainSettings = new Settings();
        private string BrowserName;
        private string BrowserVersion;
        private TabControl TabControl;
        private List<Tab> Tabs = new List<Tab>();
        private WebView2 InterfaceWebView;
        private const int TopBarHeight = 150;
        private bool IsWebViewInitialized = false;
        private Queue<Action> ScriptQueue = new Queue<Action>();
        private GlobalKeyboardHook _globalKeyboardHook;

        private Args args;

        private string DefaultURL;
        private string DefaultEngine;
        public string UserDataFolder;

        // Create a temporary directory for the interface WebView
        string tempUserDataFolder;
        private List<string> SearchHistory = new List<string>();
        private static string AppDataFolder =>
            Path.Combine(Application.LocalUserAppDataPath, "OceanBrowser");
        private static string HistoryFilePath => Path.Combine(AppDataFolder, "history.txt");


        private string PluginFolder = Path.Combine(AppDataFolder, "Plugins");
        private List<string> Plugins = new List<string>();    
        public void LoadPluginJsFiles()
        {
            List<string> jsFiles = new List<string>();

            if (Directory.Exists(PluginFolder))
            {
                string[] jsFilePaths = Directory.GetFiles(PluginFolder, "*.js", SearchOption.AllDirectories);
                jsFiles.AddRange(jsFilePaths);
            }
            else
            {
                Directory.CreateDirectory(PluginFolder);
            }

            Plugins = jsFiles;
        }


        // Bookmarks
        private void AddBookmark(string title, string url)
        {
            Bookmark bookmark = new Bookmark { Title = title, Url = url };
            Bookmark ExistingBookmark = Bookmarks.Find(b =>
                b.Url.Equals(url.Trim(), StringComparison.OrdinalIgnoreCase)
            );
            if (ExistingBookmark == null)
            {
                Bookmarks.Add(bookmark);
                SaveBookmarks();
                ShowBookmarks();
                Console.WriteLine($"Bookmark added: {title}");
            }
        }

        private void RemoveBookmark(string url)
        {
            // Find the bookmark to remove
            Bookmark bookmarkToRemove = Bookmarks.Find(b =>
                b.Url.Equals(url.Trim(), StringComparison.OrdinalIgnoreCase)
            );

            if (bookmarkToRemove != null)
            {
                Bookmarks.Remove(bookmarkToRemove);
                Console.WriteLine("Bookmark removed successfully.");
            }
            else
            {
                Console.WriteLine("Bookmark not found.");
            }
            SaveBookmarks();
            ShowBookmarks();
        }

        private void LoadBookmarks()
        {
            string bookmarksFilePath = Path.Combine(AppDataFolder, "bookmarks.json");
            if (File.Exists(bookmarksFilePath))
            {
                string json = File.ReadAllText(bookmarksFilePath);
                Bookmarks =
                    JsonConvert.DeserializeObject<List<Bookmark>>(json) ?? new List<Bookmark>();
            }
            ShowBookmarks();
        }

        private void SaveBookmarks()
        {
            string bookmarksFilePath = Path.Combine(AppDataFolder, "bookmarks.json");
            string json = JsonConvert.SerializeObject(Bookmarks);
            File.WriteAllText(bookmarksFilePath, json);
        }

        // Settings
        private void LoadSettings()
        {
            string settingspath = Path.Combine(AppDataFolder, "settings.json");
            if (File.Exists(settingspath))
            {
                string json = File.ReadAllText(settingspath);
                MainSettings = JsonConvert.DeserializeObject<Settings>(json) ?? new Settings();
            }
        }

        private void SaveSettings()
        {
            string settingspath = Path.Combine(AppDataFolder, "settings.json");
            string json = JsonConvert.SerializeObject(MainSettings);
            File.WriteAllText(settingspath, json);
        }

        private void BookmarkCurrentPage()
        {
            if (TabControl.SelectedTab != null)
            {
                Tab selectedTab = (Tab)TabControl.SelectedTab.Tag;
                string title = this.Text.Replace(BrowserName + " - ", "");
                string url = selectedTab.Source;
                AddBookmark(title, url);
                SaveBookmarks();
                ShowBookmarks();
            }
        }

        // Search History
        private void SaveSearchHistory()
        {
            try
            {
                Directory.CreateDirectory(AppDataFolder);
                File.WriteAllLines(HistoryFilePath, SearchHistory);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving search history: {ex.Message}");
            }
        }

        private void LoadSearchHistory()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    SearchHistory = new List<string>(File.ReadAllLines(HistoryFilePath));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading search history: {ex.Message}");
            }
        }

        public void ClearSearchHistory()
        {
            SearchHistory.Clear();
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    File.Delete(HistoryFilePath);
                }
                Console.WriteLine("Search history cleared successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing search history: {ex.Message}");
            }
        }

        // Constructor
        public PageManager(
            string browsername,
            string BrowserVersion,
            Args args,
            string tempUserDataFolder
        )
        {
            this.args = args;
            this.tempUserDataFolder = tempUserDataFolder;
            Init(browsername, BrowserVersion);
        }

        // Initialization
        async void Init(string browsername, string browserversion)
        {
            BrowserName = browsername;
            BrowserVersion = browserversion;
            this.Icon = new Icon(Path.Combine(Application.StartupPath, "./src/logo.ico"));
            this.Text = browsername;

            // Initialize the global keyboard hook
            _globalKeyboardHook = new GlobalKeyboardHook();
            _globalKeyboardHook.KeyPressed += InputManager;
            _globalKeyboardHook.KeyReleased += InputManagerReleased;
            _globalKeyboardHook.Hook();

            // Load settings, search history, and bookmarks
            LoadPluginJsFiles();
            LoadSettings();
            LoadSearchHistory();
            LoadBookmarks();

            // Initialize the form components
            InitializeComponent();

            // Inform the user about the temporary directory
            Console.WriteLine($"Temporary directory: {tempUserDataFolder}");

            // Check if a custom user data path is provided
            if (!string.IsNullOrEmpty(args.UserDataPath))
            {
                Console.WriteLine($"Using custom user data path: {args.UserDataPath}");
                UserDataFolder = args.UserDataPath;
            }
            else
            {
                UserDataFolder = Path.Combine(AppDataFolder, "UserData");
                Console.WriteLine($"Using default user data path: {UserDataFolder}");
            }
            // Make sure it exists
            if (!Directory.Exists(UserDataFolder))
            {
                Directory.CreateDirectory(UserDataFolder);
            }

            // Initialize the tab control
            InitializeTabControl();

            // Initialize the WebView2 control
            InitializeWebView();

            // Wait for the WebView to initialize
            await WaitForWebViewInitialization(InterfaceWebView);
            await Task.Delay(500);

            // Set useragent for the InterfaceWebView
            ConfigureUserAgent(InterfaceWebView);

            // Handle args that need to be processed after initialization

            // Check if the startup URL is valid
            bool LoadDefault = true;
            if (
                !string.IsNullOrEmpty(args.StartupUrl)
                && Uri.IsWellFormedUriString(args.StartupUrl, UriKind.Absolute)
            )
            {
                Console.WriteLine($"Starting with URL: {args.StartupUrl}");
                AddNewTab(args.StartupUrl);
                LoadDefault = false;
            }

            //PostInitalization Events
            ExecuteScript($"SetDefault('{MainSettings.DefaultSearchEngine}', {LoadDefault.ToString().ToLower()});");
            ShowBookmarks();
        }


        private bool IsApplicationFocused()
        {
            IntPtr activeWindow = GetForegroundWindow();

            return activeWindow == this.Handle;
        }

        private bool ControlPressed = false;
        private bool NewTabRegistered = false;
        private bool CloseTabRegistered = false;
        private bool HistoryTabRegistered = false; // New variable for Control + H

        private void InputManagerReleased(object sender, Keys key)
        {
            if (key == Keys.LControlKey)
            {
                ControlPressed = false;
            }
            if (key == Keys.W)
            {
                CloseTabRegistered = false;
            }
            if (key == Keys.N)
            {
                NewTabRegistered = false;
            }
            if (key == Keys.H)
            {
                HistoryTabRegistered = false;
            }
        }

        private void InputManager(object sender, Keys key)
        {
            if (!IsApplicationFocused())
                return;

            if (key == Keys.LControlKey)
            {
                ControlPressed = true;
            }
            if (ControlPressed)
            {
                if (key == Keys.N && !NewTabRegistered)
                {
                    DefaultTab();
                    NewTabRegistered = true;
                }
                else if (key == Keys.W && !CloseTabRegistered)
                {
                    if (TabControl.SelectedTab != null)
                    {
                        CloseTab(((Tab)(TabControl.SelectedTab.Tag)).Id);
                        CloseTabRegistered = true;
                    }
                }
                else if (key == Keys.H && !HistoryTabRegistered)
                {
                    OpenSettingsDocument(true);
                    HistoryTabRegistered = true;
                }
            }
        }


            private void InitializeTabControl()
        {
            TabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                ItemSize = new Size(0, 1),
                SizeMode = TabSizeMode.Fixed,
                Appearance = TabAppearance.Buttons,
            };
            TabControl.DrawMode = TabDrawMode.OwnerDrawFixed;

            TabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            this.Controls.Add(TabControl);
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TabControl.SelectedTab != null)
            {
                Tab selectedTab = (Tab)TabControl.SelectedTab.Tag;
                if (selectedTab != null)
                {
                    selectedTab.WebView.Visible = true;
                    TabControl.SelectedTab.BackColor = Color.LightGray;
                }

                foreach (TabPage tabPage in TabControl.TabPages)
                {
                    if (tabPage != TabControl.SelectedTab)
                    {
                        Tab tab = (Tab)tabPage.Tag;
                        tab.WebView.Visible = false;
                    }
                }

                UpdateTabInterface();
            }
        }

        private async void InitializeWebView()
        {
            Directory.CreateDirectory(tempUserDataFolder);

            // Create the environment for the interface WebView with the temporary folder
            var environment = await CoreWebView2Environment.CreateAsync(null, tempUserDataFolder);

            InterfaceWebView = new WebView2
            {
                Dock = DockStyle.Top,
                Height = TopBarHeight,
                DefaultBackgroundColor = Color.FromArgb(0, 0, 0),
            };

            this.Controls.Add(InterfaceWebView);
            await InterfaceWebView.EnsureCoreWebView2Async(environment);

            // Disable the zoom controls
            InterfaceWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            InterfaceWebView.CoreWebView2.Settings.IsPinchZoomEnabled = false;

            // Enable the dev tools
            //InterfaceWebView.CoreWebView2.OpenDevToolsWindow();

            // Handle the web message received event
            InterfaceWebView.WebMessageReceived += InterfaceWebView_WebMessageReceived;
            InterfaceWebView.CoreWebView2.NewWindowRequested += InterfaceWebView_NavigationStarting;

            // Load the interface HTML file
            string htmlFilePath = Path.Combine(
                Application.StartupPath,
                "./src/interface/interface.html"
            );
            InterfaceWebView.Source = new Uri(htmlFilePath);
            IsWebViewInitialized = true;
            ProcessScriptQueue();
        }

        private void InterfaceWebView_WebMessageReceived(
            object sender,
            CoreWebView2WebMessageReceivedEventArgs args
        )
        {
            string message = args.TryGetWebMessageAsString();
            Console.WriteLine($"Received message: {message}");

            if (message.StartsWith("newtab:"))
            {
                string url = message.Substring(7);
                if (url.Equals("browser://settings", StringComparison.OrdinalIgnoreCase))
                {
                    OpenSettingsDocument(true);
                }
                else
                {
                    AddNewTab(url);
                }
            }
            else if (message.StartsWith("searchenginedefault:"))
            {
                string enginename = message.Substring(20);
                MainSettings.DefaultSearchEngine = enginename;
                SaveSettings();
                SetSearchEngine(enginename);
            }
            else if (message.StartsWith("searchengine:"))
            {
                string enginename = message.Substring(13);
                SetSearchEngine(enginename);
            }
            else if (message.StartsWith("newdefault:"))
            {
                DefaultTab();
            }
            else if (message.StartsWith("switchtab:"))
            {
                string tabId = message.Substring(10);
                SwitchToTab(tabId);
            }
            else if (message.StartsWith("closetab:"))
            {
                string tabId = message.Substring(9);
                CloseTab(tabId);
            }
            else if (message.StartsWith("back:"))
            {
                string tabId = message.Substring(5);
                Back(tabId);
            }
            else if (message.StartsWith("forward:"))
            {
                string tabId = message.Substring(8);
                Forward(tabId);
            }
            else if (message.StartsWith("reload:"))
            {
                string tabId = message.Substring(7);
                Reload(tabId);
            }
            else if (message.StartsWith("addbookmark:"))
            {
                BookmarkCurrentPage();
            }
            else if (message.StartsWith("removebookmark:"))
            {
                string url = message.Substring(15);
                RemoveBookmark(url);
            }
            else
            {
                // Perform search or navigate based on the message
                PerformSearch(message);
            }
        }

        void DefaultTab()
        {
            try
            {
                AddNewTab(DefaultURL);
            }
            catch { }
        }

        private void ConfigureUserAgent(WebView2 webView)
        {
            if (webView.CoreWebView2 != null)
            {
                string defaultUserAgent = webView.CoreWebView2.Settings.UserAgent;
                webView.CoreWebView2.Settings.UserAgent =
                    $"{defaultUserAgent} {BrowserName}/{BrowserVersion}";
            }
            else
            {
                Console.WriteLine("CoreWebView2 is not initialized yet.");
            }
        }

        public async Task SendSearchHistoryToSettings()
        {
            try
            {
                string jsonHistory = JsonConvert.SerializeObject(SearchHistory);
                // Ensure that the WebView is initialized
                var selectedTab = (Tab)(TabControl.SelectedTab.Tag);
                if (selectedTab?.WebView?.CoreWebView2 != null)
                {
                    await selectedTab.WebView.CoreWebView2.ExecuteScriptAsync(
                        $"UpdateSearchHistory('{jsonHistory}');"
                    );
                }
                else
                {
                    Console.WriteLine("WebView is not initialized.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending search history: {ex.Message}");
            }
        }

        private async void SettingsMessageReceived(
            object sender,
            CoreWebView2WebMessageReceivedEventArgs args
        )
        {
            string message = args.TryGetWebMessageAsString();
            Console.WriteLine($"Received message: {message}");
            if (message == "SettingsLoaded")
            {
                Console.WriteLine(message);
                await SendSearchHistoryToSettings();
            }
            if (message == "ClearHistory")
            {
                ClearSearchHistory();
                await SendSearchHistoryToSettings();
            }
            if (message == "ClearCache")
            {
                ClearCache();
                MessageBox.Show("Cache cleared successfully.");
            }
            if (message == "ClearCookies")
            {
                ClearCookies();
                MessageBox.Show("Cookies cleared successfully.");
            }
            if (message.StartsWith("searchenginedefault:"))
            {
                string enginename = message.Substring(20);
                MainSettings.DefaultSearchEngine = enginename;
                SaveSettings();
                SetSearchEngine(enginename);
            }
        }

        private async void ClearCache()
        {
            try
            {
                var selectedTab = (Tab)(TabControl.SelectedTab.Tag);
                await selectedTab.WebView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "Network.clearBrowserCache",
                    "{}"
                );
                Console.WriteLine("Cache cleared successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cache: {ex.Message}");
            }
        }

        private void ClearCookies()
        {
            try
            {
                var selectedTab = (Tab)(TabControl.SelectedTab.Tag);
                var cookieManager = selectedTab.WebView.CoreWebView2.CookieManager;
                cookieManager.DeleteAllCookies();
                Console.WriteLine("Cookies cleared successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing cookies: {ex.Message}");
            }
        }

        private async void OpenSettingsDocument(bool newtab)
        {
            string settingsHtmlPath = Path.Combine(
                Application.StartupPath,
                "./src/settings/settings.html"
            );
            if (newtab)
            {
                AddNewTab(settingsHtmlPath);
                await Task.Delay(1);
            }

            if (TabControl.SelectedTab != null)
            {
                Tab activeTab = (Tab)TabControl.SelectedTab.Tag;
                activeTab.WebView.CoreWebView2.WebMessageReceived += SettingsMessageReceived;
                activeTab.WebView.Source = new Uri(settingsHtmlPath);
            }
            else
            {
                AddNewTab(settingsHtmlPath);
            }
        }

        private void SetSearchEngine(string engine)
        {
            DefaultEngine = engine;
            switch (engine.ToLower())
            {
                case "google":
                    DefaultURL = "https://google.com/";
                    break;
                case "bing":
                    DefaultURL = "https://www.bing.com/";
                    break;
                case "duckduckgo":
                    DefaultURL = "https://www.duckduckgo.com/";
                    break;
                case "yahoo":
                    DefaultURL = "https://www.yahoo.com/";
                    break;
                case "brave":
                    DefaultURL = "https://search.brave.com/";
                    break;
                default:
                    DefaultURL = "https://google.com/";
                    break;
            }
        }

        private void PerformSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return;

            if (searchText.Equals("browser://settings", StringComparison.OrdinalIgnoreCase))
            {
                OpenSettingsDocument(false);
                return;
            }

            Uri uriResult;
            bool isValidUrl = Uri.TryCreate(searchText, UriKind.Absolute, out uriResult);
            if (searchText.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.TryCreate(searchText, UriKind.Absolute, out uriResult) && File.Exists(uriResult.LocalPath))
                {
                    if (TabControl.SelectedTab != null)
                    {
                        Tab activeTab = (Tab)TabControl.SelectedTab.Tag;
                        activeTab.WebView.Source = uriResult;
                    }
                }
                else
                {
                    MessageBox.Show("The specified file does not exist.");
                }
                return;
            }

            // Check if the searchText is a domain (not a complete URL)
            if (IsDomain(searchText))
            {
                string domainUrl = "https://" + searchText; // Add https:// if it's a domain
                if (Uri.TryCreate(domainUrl, UriKind.Absolute, out uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    if (TabControl.SelectedTab != null)
                    {
                        Tab activeTab = (Tab)TabControl.SelectedTab.Tag;
                        activeTab.WebView.Source = uriResult;
                    }
                    return;
                }
            }

            if (isValidUrl)
            {
                // Handle HTTP(S) or file URLs
                if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeFile)
                {
                    if (TabControl.SelectedTab != null)
                    {
                        Tab activeTab = (Tab)TabControl.SelectedTab.Tag;
                        activeTab.WebView.Source = uriResult;
                    }
                }
                else
                {
                    MessageBox.Show("Unsupported URL scheme.");
                }
            }
            else
            {
                // If not a URL, perform a search using the default engine
                string searchUrl;
                switch (DefaultEngine.ToLower())
                {
                    case "bing":
                        searchUrl = $"https://www.bing.com/search?q={Uri.EscapeDataString(searchText)}";
                        break;
                    case "duckduckgo":
                        searchUrl = $"https://duckduckgo.com/?q={Uri.EscapeDataString(searchText)}";
                        break;
                    case "yahoo":
                        searchUrl = $"https://search.yahoo.com/search?p={Uri.EscapeDataString(searchText)}";
                        break;
                    case "brave":
                        searchUrl = $"https://search.brave.com/search?q={Uri.EscapeDataString(searchText)}";
                        break;
                    case "google":
                    default:
                        searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(searchText)}";
                        break;
                }

                if (TabControl.SelectedTab != null)
                {
                    Tab activeTab = (Tab)TabControl.SelectedTab.Tag;
                    activeTab.WebView.Source = new Uri(searchUrl);
                }
            }
        }



        private bool IsDomain(string input)
        {
            return input.Contains(".")
                && !input.StartsWith("http://")
                && !input.StartsWith("https://");
        }

        private async void AddNewTab(string url)
        {
            WebView2 newWebView = new WebView2
            {
                DefaultBackgroundColor = Color.FromArgb(255, 255, 255),
            };
            InitializeWebViewInstance(newWebView, url);

            string tabId = Guid.NewGuid().ToString();
            string title = new Uri(url).Host;

            Tab newTab = new Tab
            {
                WebView = newWebView,
                Id = tabId,
            };

            Tabs.Add(newTab);

            TabPage tabPage = new TabPage(title) { Tag = newTab };
            tabPage.Controls.Add(newWebView);
            TabControl.TabPages.Add(tabPage);

            newWebView.NavigationCompleted += (s, e) => UpdateTabOnNavigation(newTab);
            UpdateTabInterface();
            SwitchToTab(newTab.Id);
            await WaitForWebViewInitialization(newWebView);
            newWebView.CoreWebView2.NewWindowRequested += InterfaceWebView_NavigationStarting;
        }

        private void InterfaceWebView_NavigationStarting(
            object sender,
            CoreWebView2NewWindowRequestedEventArgs e
        )
        {
            AddNewTab(e.Uri);
            e.Handled = true;
        }

        private async void InitializeWebViewInstance(WebView2 webView, string url)
        {
            webView.Dock = DockStyle.Fill;
            Console.WriteLine($"Initializing WebView for URL: {url}");
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(
                userDataFolder: UserDataFolder
            );
            await webView.EnsureCoreWebView2Async(environment);
            ConfigureUserAgent(webView);
            webView.Source = new Uri(url);
        }

        private async void UpdateTabOnNavigation(Tab tab)
        {
            string settingsHtmlPath = Path.Combine(Application.StartupPath, "./src/settings/settings.html");
            settingsHtmlPath = new Uri(settingsHtmlPath).ToString();

            await WaitForWebViewInitialization(tab.WebView);

            tab.Source = tab.WebView.CoreWebView2.Source;

            string source = tab.WebView.CoreWebView2.Source;
            if (source == settingsHtmlPath)
            {
                source = "browser://settings";
            }
            SearchHistory.Insert(0, source + " - " + DateTime.Now);

            void OnSourceChanged(object sender, EventArgs e)
            {
                UpdateSource(tab, settingsHtmlPath, true);
            }

            void OnDocumentTitleChanged(object sender, object e)
            {
                UpdateSource(tab, settingsHtmlPath, false);
            }

            tab.WebView.CoreWebView2.DocumentTitleChanged += OnDocumentTitleChanged;
            tab.WebView.CoreWebView2.SourceChanged += OnSourceChanged;

            UpdateTabInterface();

            if (SearchHistory.Count > 1000)
            {
                SearchHistory.RemoveAt(SearchHistory.Count - 1);
            }

            InjectPlugins(tab.WebView.CoreWebView2);
            SaveSearchHistory();
        }
        private void UpdateSource(Tab tab, string settingsHtmlPath, bool injectplugins)
        {
            string source = tab.WebView.CoreWebView2.Source;
            if (source == settingsHtmlPath)
            {
                source = "browser://settings";
            }
            tab.Source = source;
            UpdateTabInterface();
            if (injectplugins)
            {
                InjectPlugins(tab.WebView.CoreWebView2);
            }
        }
        void InjectPlugins(CoreWebView2 TabView)
        {
            foreach (string PluginPath in Plugins)
            {
                try
                {
                    Console.WriteLine("Plugin Injected: " + PluginPath);
                    string Plugin = File.ReadAllText(PluginPath);
                    TabView.ExecuteScriptAsync(Plugin);
                }
                catch
                {

                }
            }
        }

        private async Task WaitForWebViewInitialization(WebView2 webView)
        {
            while (webView.CoreWebView2 == null)
            {
                await Task.Delay(100);
            }
        }

        private void SwitchToTab(string tabId)
        {
            foreach (TabPage tabPage in TabControl.TabPages)
            {
                if (((Tab)tabPage.Tag).Id == tabId)
                {
                    TabControl.SelectedTab = tabPage as TabPage;
                    break;
                }
            }
            UpdateTabInterface();
        }

        private void CloseTab(string tabId)
        {
            Tab tabToClose = Tabs.Find(t => t.Id == tabId);
            if (tabToClose != null)
            {
                // Remove the WebView2 instance
                tabToClose.WebView.Dispose();

                Tabs.Remove(tabToClose);
                foreach (TabPage tabPage in TabControl.TabPages)
                {
                    if (((Tab)tabPage.Tag).Id == tabId)
                    {
                        ExecuteScript($"RemoveTab('{tabId}');");
                        TabControl.TabPages.Remove(tabPage as TabPage);
                        UpdateTabInterface();
                        break;
                    }
                }
            }

            // If no tabs are left, open a new default tab
            if (TabControl.TabPages.Count == 0)
            {
                DefaultTab();
            }
        }

        private void Back(string tabId)
        {
            Tab tab = Tabs.Find(t => t.Id == tabId);
            if (tab != null && tab.WebView.CoreWebView2.CanGoBack)
            {
                tab.WebView.CoreWebView2.GoBack();
            }
        }

        private void Forward(string tabId)
        {
            Tab tab = Tabs.Find(t => t.Id == tabId);
            if (tab != null && tab.WebView.CoreWebView2.CanGoForward)
            {
                tab.WebView.CoreWebView2.GoForward();
            }
        }

        private void Reload(string tabId)
        {
            Tab tab = Tabs.Find(t => t.Id == tabId);
            if (tab != null)
            {
                tab.WebView.CoreWebView2.Reload();
            }
        }

        private void ShowBookmarks()
        {
            string json = JsonConvert.SerializeObject(Bookmarks);
            ExecuteScript($"AddBookmarks('{json}')");
        }

        private void UpdateTabInterface()
        {
            foreach (Tab tab in Tabs)
            {
                ExecuteScript($"AddTab('{tab.Id}');");

                string title;
                string rawTitle;

                try
                {
                    title = SanitizeString(tab.WebView.CoreWebView2.DocumentTitle);
                    rawTitle = tab.WebView.CoreWebView2.DocumentTitle;
                }
                catch
                {
                    title = tab.Source;
                    rawTitle = tab.Source;
                }

                if (string.IsNullOrEmpty(title) || title == "")
                {
                    title = tab.Source;
                    rawTitle = tab.Source;
                }

                // Check if the current tab is the settings page
                string settingsHtmlPath = Path.Combine(
                    Application.StartupPath,
                    "./src/settings/settings.html"
                );

                if (tab.Source == new Uri(settingsHtmlPath).ToString())
                {
                    ExecuteScript(
                        $"UpdateTabTitle('{tab.Id}', 'Settings', 'browser://settings');"
                    );
                }
                else
                {
                    ExecuteScript($"UpdateTabTitle('{tab.Id}', '{title}', '{tab.Source}');");
                }
            }

            // Update title of the main window to the title of the selected tab
            Tab selectedTab = (Tab)TabControl.SelectedTab?.Tag;
            if (selectedTab != null)
            {
                ExecuteScript($"UpdateActiveTab('{selectedTab.Id}');");

                string selectedRawTitle;
                try
                {
                    selectedRawTitle = selectedTab.WebView.CoreWebView2.DocumentTitle;
                }
                catch
                {
                    selectedRawTitle = selectedTab.Source;
                }

                this.Text = BrowserName + " - " + selectedRawTitle;
            }
        }


        public static string SanitizeString(string input)
        {
            // Escape special characters for JavaScript string
            if (input == null) return string.Empty;

            return input
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r") 
                .Replace("\t", "\\t")
                .Replace("\b", "\\b") 
                .Replace("\f", "\\f");
        }


        private async void ExecuteScript(string script)
        {
            if (IsWebViewInitialized)
            {
                    try
                    {
                        await InterfaceWebView.CoreWebView2.ExecuteScriptAsync(script);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error executing script: {ex.Message}");
                    }
            }
            else
            {
                ScriptQueue.Enqueue(() => ExecuteScript(script));
            }
        }




        private async void ProcessScriptQueue()
        {
            while (ScriptQueue.Count > 0)
            {
                var scriptAction = ScriptQueue.Dequeue();
                await Task.Run(scriptAction);
            }
        }


        public void PageManager_Load(object sender, EventArgs e) { }

        private static void CleanupTempDirectory(string tempUserDataFolder)
        {
            try
            {
                Console.WriteLine("Deleting temporary directory...");
                if (Directory.Exists(tempUserDataFolder))
                {
                    try
                    {
                        Console.WriteLine("Deleting temporary directory: " + tempUserDataFolder);
                        Directory.Delete(tempUserDataFolder, true); // Delete the temp directory and its contents
                        Console.WriteLine("Temporary directory deleted.");
                    }
                    catch (IOException ioEx)
                    {
                        Console.WriteLine($"Error during deletion: {ioEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected error during deletion: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Temporary directory does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        // Override the FormClosing event to perform cleanup
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            this.Hide();
            if (InterfaceWebView != null)
            {
                InterfaceWebView.Dispose();
                InterfaceWebView = null;
            }
            foreach (Tab tab in Tabs)
            {
                tab.WebView.Dispose();
            }
            TabControl.Dispose();
            _globalKeyboardHook.Unhook();
            Task.Delay(1000).Wait();
            CleanupTempDirectory(tempUserDataFolder);

            base.OnFormClosing(e);
        }
    }
}
