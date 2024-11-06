const TabContainer = document.getElementById('TabContainer');
const UrlInput = document.getElementById('UrlInput');
const SearchButton = document.getElementById('SearchButton');
const NewTabButton = document.getElementById('NewTabButton');
const BackButton = document.getElementById('BackButton');
const ForwardButton = document.getElementById('ForwardButton');
const SettingsButton = document.getElementById('SettingsButton');
const ReloadButton = document.getElementById('ReloadButton');
const BookmarkContainer = document.getElementById('BookmarkContainer');
const AddBookmarkButton = document.getElementById('AddBookmarkButton');

let activeTabIds = [];

function CreateButton(innerText, className, onClick) {
    const button = document.createElement('button');
    button.innerText = innerText;
    button.className = className;
    button.onclick = onClick;
    return button;
}

// Function to create a generic tab or bookmark item
function CreateItem(container, className, title, id, isTab = true, url = '') {
    const item = document.createElement('div');
    item.className = className;
    item.id = id;
    item.style.display = 'flex';

    const titleElement = document.createElement('span');
    titleElement.className = 'Title';
    titleElement.innerText = title;
    item.appendChild(titleElement);

    const button = CreateButton('X', isTab ? 'CloseButton' : 'RemoveButton', (event) => {
        event.stopPropagation();
        if (isTab) {
            window.chrome.webview.postMessage(`closetab:${id}`);
        } else {
            HandleRemoveBookmark(url);
        }
    });

    item.appendChild(button);

    item.onclick = () => {
        if (isTab) {
            window.chrome.webview.postMessage(`switchtab:${id}`);
        } else {
            window.chrome.webview.postMessage(`newtab:${url}`);
        }
    };

    container.appendChild(item);
}

NewTabButton.onclick = () => {
    window.chrome.webview.postMessage(`newdefault:`);
};

ReloadButton.onclick = () => {
    const activeTabId = GetActiveTabId();
    if (activeTabId) {
        window.chrome.webview.postMessage(`reload:${activeTabId}`);
    }
};

SearchButton.onclick = () => {
    const query = UrlInput.value;
    window.chrome.webview.postMessage(query);
};

BackButton.onclick = () => {
    const activeTabId = GetActiveTabId();
    if (activeTabId) {
        window.chrome.webview.postMessage(`back:${activeTabId}`);
    }
};

ForwardButton.onclick = () => {
    const activeTabId = GetActiveTabId();
    if (activeTabId) {
        window.chrome.webview.postMessage(`forward:${activeTabId}`);
    }
};

SettingsButton.onclick = () => {
    window.chrome.webview.postMessage(`newtab:browser://settings`);
};

function GetActiveTabId() {
    const activeTab = TabContainer.getElementsByClassName('Active')[0];
    return activeTab ? activeTab.id : null;
}

// Adds a new tab, ensuring no duplicate IDs
function AddTab(tabId) {
    if (!activeTabIds.includes(tabId)) {
        activeTabIds.push(tabId);
        CreateItem(TabContainer, 'Tab', "", tabId, true);
    }
}

// Removes tab both from the activeTabIds array and DOM
function RemoveTab(tabId) {
    const index = activeTabIds.indexOf(tabId);
    if (index !== -1) {
        activeTabIds.splice(index, 1);
        const tabElement = document.getElementById(tabId);
        if (tabElement) {
            TabContainer.removeChild(tabElement);
        }
    }
}

// Removes tabs that are not in activeTabIds
function ClearTabs() {
    const tabs = Array.from(TabContainer.children);
    tabs.forEach(tab => {
        if (!activeTabIds.includes(tab.id)) {
            TabContainer.removeChild(tab);
        }
    });
}

// Updates the active tab by adding/removing the 'Active' class
function UpdateActiveTab(activeTabId) {
    const tabs = TabContainer.getElementsByClassName('Tab');
    Array.from(tabs).forEach(tab => {
        tab.classList.remove('Active');
    });
    const activeTab = Array.from(tabs).find(tab => tab.id === activeTabId);
    if (activeTab) activeTab.classList.add('Active');
}

// Updates the title of a specific tab and updates the URL input
function UpdateTabTitle(tabId, newTitle, urlVal) {
    console.log(newTitle);  
    const tabs = TabContainer.getElementsByClassName('Tab');
    const tabToUpdate = Array.from(tabs).find(tab => tab.id === tabId);
    if (tabToUpdate) {  
        const titleElement = tabToUpdate.querySelector('span');
        titleElement.textContent = newTitle.toString();
        UrlInput.value = urlVal;
    }
}

// Keyboard event listeners for Enter, Back, and Forward
document.addEventListener('keydown', (event) => {
    if (event.key === 'Enter') {
        event.preventDefault();
        SearchButton.onclick();
    }
    if (event.key === 'F5' || (event.ctrlKey && event.key === 'r')) {
        event.preventDefault();
    }
});
document.addEventListener('mousedown', (event) => {
    if (event.button === 3) {
        BackButton.onclick();
    }
    if (event.button === 4) {
        ForwardButton.onclick();
    }
});
document.addEventListener('contextmenu', (event) => {
    event.preventDefault();
});

// Bookmark management functions
function AddBookmark(bookmark) {
    CreateItem(BookmarkContainer, 'Bookmark', bookmark.Title, bookmark.Url, false, bookmark.Url);
}

function ClearBookmarks() {
    BookmarkContainer.innerHTML = '';
}

function AddBookmarks(bookmarksJson) {
    const bookmarks = JSON.parse(bookmarksJson);
    ClearBookmarks();
    bookmarks.forEach(bookmark => {
        AddBookmark(bookmark);
    });
}

function HandleAddBookmark() {
    const currentUrl = UrlInput.value;
    if (currentUrl) {
        window.chrome.webview.postMessage(`addbookmark:`);
    }
}

function HandleRemoveBookmark(url) {
    window.chrome.webview.postMessage(`removebookmark:${url}`);
    const bookmarks = Array.from(BookmarkContainer.children);
    bookmarks.forEach(bookmarkItem => {
        if (bookmarkItem.innerText.includes(url)) {
            BookmarkContainer.removeChild(bookmarkItem);
        }
    });
}

AddBookmarkButton.onclick = HandleAddBookmark;

window.chrome.webview.addEventListener('message', (event) => {
    const message = event.data;
    if (message.startsWith('AddBookmarks:')) {
        const bookmarksJson = message.substring(13);
        AddBookmarks(bookmarksJson);
    }
});

function SetDefault(DefaultEngine, OpenDefault) {
    console.log("Setting Default: " + DefaultEngine);
    if (!DefaultEngine) {
        DefaultEngine = "brave";
    }
    window.chrome.webview.postMessage(`searchengine:${DefaultEngine}`);
    if (OpenDefault) {
        window.chrome.webview.postMessage(`newdefault:`);
    }
}
