var editableElements = [];

document.addEventListener('click', document.fnEditMode=function (event) {
    if (!editableElements.includes(event.target))
        editableElements.push(event.target);

    event.target.contentEditable = true;  
});