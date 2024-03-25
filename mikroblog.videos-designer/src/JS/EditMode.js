var editableElements = [];

// Adds on click event listener to the document. When any element will be clicked, its content will become editable.
function enableEditMode() {
    document.addEventListener('click', document.fnEditMode = function (event) {
        if (!editableElements.includes(event.target))
            editableElements.push(event.target);

        event.target.contentEditable = true;
    });
}

// Removes content editablity from all elements in editableElements lis, removes on click event listener from the document and clear editableElements list.
function disableEditMode() {
    editableElements.forEach((element) => {
        element.contentEditable = false;
    })

    document.removeEventListener('click', document.fnEditMode);

    editableElements = [];
}