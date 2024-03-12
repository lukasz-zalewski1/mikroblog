if (typeof entries === 'undefined')
    var entries = [];

let topHeader = document.querySelector("header.header");
if (topHeader !== null)
    topHeader.remove();

document.querySelectorAll("section[data-label^='ad:']").forEach(e => e.remove());

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

function disableDesignerMode() {
    document.removeEventListener('click', document.fnDesignerMode);
}

function addNewEntry(entryNode) {
    let numberNode = document.createElement("p");
    numberNode.style = "font-size:32px; background-color:red;"
    numberNode.innerText = entries.length + 1;

    entryNode.insertBefore(numberNode);

    entries.push({ node: entryNode, numberNode: numberNode });   

    sendEntriesNumberToWebView();
}

function removeEntry(entryNode) {
    let obj = entries.find(o => o.node === entryNode);

    obj.node.removeChild(obj.numberNode);

    entries = entries.filter((e) => e.node !== entryNode);

    for (let i = 0; i < entries.length; i += 1) {
        entries[i].numberNode.innerText = i + 1;
    }

    sendEntriesNumberToWebView();
}

function sendEntriesNumberToWebView() {
    let json = {
        message: "EntriesLength",
        value: entries.length
    }

    window.chrome.webview.postMessage(json);
}

function sendScreenshotData(entryNumber) {
    let rect = entries[entryNumber].node.getBoundingClientRect();

    let json = {
        message: "ScreenshotData",
        entryNumber: entryNumber,
        x: rect.x,
        y: rect.y,
        width: rect.width,
        height: rect.height,
    }

    window.chrome.webview.postMessage(json);
}

function hideEntryNumberNode(entryNumber) {
    entries[entryNumber].numberNode.hidden = true;

    window.scroll(0, entries[entryNumber].node.getBoundingClientRect().top + document.documentElement.scrollTop);
}

function showEntryNumberNode(entryNumber) {
    entries[entryNumber].numberNode.hidden = false;
}

function cleanEntries() {
    entries = [];
}

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
