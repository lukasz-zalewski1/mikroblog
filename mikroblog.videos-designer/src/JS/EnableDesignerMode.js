if (typeof entries === 'undefined')
    var entries = [];

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


function addNewEntry(entryNode) {
    let numberNode = document.createElement("p");
    numberNode.style = "font-size:32px; background-color:red;"
    numberNode.innerText = entries.length + 1;

    entryNode.insertBefore(numberNode);

    entries.push({ node: entryNode, numberNode: numberNode });   

    sendEntriesToWebView();
}

function removeEntry(entryNode) {
    let obj = entries.find(o => o.node === entryNode);

    obj.node.removeChild(obj.numberNode);

    entries = entries.filter((e) => e.node !== entryNode);

    for (let i = 0; i < entries.length; i += 1) {
        entries[i].numberNode.innerText = i + 1;
    }

    sendEntriesToWebView();
}

function sendEntriesToWebView() {
    let array = []

    entries.forEach((e) => {
        let rect = e.node.getBoundingClientRect();
        array.push({
            x: rect.x,
            y: rect.y,
            width: rect.width,
            height: rect.height,
            text: e.node.querySelector("div.edit-wrapper").innerText
        })
    })

    console.log(array);

    window.chrome.webview.postMessage(JSON.parse(JSON.stringify(array)));
}