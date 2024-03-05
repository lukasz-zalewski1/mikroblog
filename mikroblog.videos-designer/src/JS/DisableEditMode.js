editableElements.forEach((element) => {
    element.contentEditable = false;
})

document.removeEventListener('click', document.fnEditMode);

editableElements = [];