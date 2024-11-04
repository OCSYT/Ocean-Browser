window.onload = function () {
    window.chrome.webview.postMessage("SettingsLoaded");
}

document.getElementById('ClearCookiesButton').addEventListener('click', function () {
    window.chrome.webview.postMessage("ClearCookies");
});

document.getElementById('ClearCacheButton').addEventListener('click', function () {
    window.chrome.webview.postMessage("ClearCache");
});

document.getElementById('ClearHistoryButton').addEventListener('click', function () {
    window.chrome.webview.postMessage("ClearHistory");
});

function UpdateSearchHistory(historyJson) {
    console.log('Received search history:', historyJson);
    try {
        const historyArray = JSON.parse(historyJson);
        const searchHistoryList = document.getElementById('SearchHistoryList');
        searchHistoryList.innerHTML = '';
        historyArray.forEach(function (item) {
            const listItem = document.createElement('li');
            listItem.textContent = item;
            searchHistoryList.appendChild(listItem);
        });
    } catch (error) {
        console.error('Error parsing search history:', error);
    }
}

function SearchEngineButtons() {
    const EngineList = document.getElementById("SearchEngineList");
    let Engines = ["google", "bing", "duckduckgo", "yahoo"];
    Engines.forEach(engine => {
        const button = document.createElement("button");
        button.innerText = engine.charAt(0).toUpperCase() + engine.slice(1);
        button.onclick = () => {
            SetDefaultSearchEngine(engine);
            console.log(`Default search engine set to: ${engine}`); 
        };
        EngineList.appendChild(button);
    });
}
SearchEngineButtons();

function SetDefaultSearchEngine(engine) {
    window.chrome.webview.postMessage("searchenginedefault:" + engine);
}
