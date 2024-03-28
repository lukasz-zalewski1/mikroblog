// Initializes list of entries
if (typeof entries === 'undefined')
    var entries = [];

// Removes unwanted notes from displayed page
let topHeader = document.querySelector("header.header");
if (topHeader !== null)
    topHeader.remove();

document.querySelectorAll("section[data-label^='ad:']").forEach(e => e.remove());
document.querySelectorAll("span.button").forEach(x => x.remove());
document.querySelectorAll("h1").forEach(x => x.remove());

let navbar = document.querySelector("nav.mobile-navbar");
if (navbar !== null)
    navbar.remove();

// Enables designer mode by adding on click event listener on document. 
// Clicked elements will be added to entries list and will have a visual representation of entryNumber beneath itself.
function enableDesignerMode() {
    document.addEventListener('click', document.fnDesignerMode = function (event) {
        let articleNode = event.target;

        while (true) {
            if (articleNode.localName === "article") {
                if (entries.some(e => e.node === articleNode)) {
                    removeEntry(articleNode);
                    return;
                }

                break;
            }

            articleNode = articleNode.parentNode;
        }

        addNewEntry(articleNode);
    });
}

// Remove on click event listener from the document.
function disableDesignerMode() {
    document.removeEventListener('click', document.fnDesignerMode);
}

// Adds new entry to entries list and adds visual representation of entryNumber beneath it.
// Later calls sendEntriesNumberToWebView() 
function addNewEntry(entryNode) {
    let numberNode = document.createElement("p");
    numberNode.style = "font-size:32px; background-color:red;"
    numberNode.innerText = entries.length + 1;

    entryNode.insertBefore(numberNode);

    entries.push({ node: entryNode, numberNode: numberNode });   

    sendEntriesNumberToWebView();
}

// Removes entry from entries list and visual representation of entryNumber.
// Later calls sendEntriesNumberToWebView()
function removeEntry(entryNode) {
    let obj = entries.find(o => o.node === entryNode);

    obj.node.removeChild(obj.numberNode);

    entries = entries.filter((e) => e.node !== entryNode);

    for (let i = 0; i < entries.length; i += 1) {
        entries[i].numberNode.innerText = i + 1;
    }

    sendEntriesNumberToWebView();
}

// Sends JsonObject with entries.length to webview.
function sendEntriesNumberToWebView() {
    let json = {
        message: "EntriesCount",
        value: entries.length
    }

    window.chrome.webview.postMessage(json);
}

// Sends JsonObject with screenshot data to webview.
function sendScreenshotData(entryNumber) {
    let rect = entries[entryNumber].node.getBoundingClientRect();

    let json = {
        message: "ScreenshotData",
        entryNumber: entryNumber,
        x: rect.x,
        y: rect.y,
        width: rect.width,
        height: rect.height,
        scaling: window.devicePixelRatio
    }

    window.chrome.webview.postMessage(json);
}

// Hides visual representation of entryNumber for a node.
function hideEntryNumberNode(entryNumber) {
    entries[entryNumber].numberNode.hidden = true;

    window.scroll(0, entries[entryNumber].node.getBoundingClientRect().top + document.documentElement.scrollTop);
}

// Shows visual representation of entryNumber beneath a node.
function showEntryNumberNode(entryNumber) {
    entries[entryNumber].numberNode.hidden = false;
}

// Removes all entries from entries list.
function cleanEntries() {
    entries = [];
}

// Sends JsonObject with speech data to webview.
function sendSpeechData(entryNumber) {
    let isMale = true;

    if (entries[entryNumber].node.querySelector("figure.female"))
        isMale = false;

    let blockquote = (entries[entryNumber].node.querySelector("div.content").querySelector("blockquote"))
    if (blockquote)
        blockquote.hidden = true;

    let json = {
        message: "SpeechData",
        entryNumber: entryNumber,
        text: entries[entryNumber].node.querySelector("div.content").innerText,
        isMale: isMale
    }

    if (blockquote)
        blockquote.hidden = false;

    window.chrome.webview.postMessage(json);
}
