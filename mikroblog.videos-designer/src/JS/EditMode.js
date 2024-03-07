var editableElements = [];

function enableEditMode() {
    document.addEventListener('click', document.fnEditMode = function (event) {
        if (!editableElements.includes(event.target))
            editableElements.push(event.target);

        event.target.contentEditable = true;
    });
}

function disableEditMode() {
    editableElements.forEach((element) => {
        element.contentEditable = false;
    })

    document.removeEventListener('click', document.fnEditMode);

    editableElements = [];
}