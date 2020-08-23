function ScrollToBottom(id) {
    var textarea = document.getElementById(id);
    textarea.scrollTop = textarea.scrollHeight;
}

function ShowNotLogin(message) {
    alert(message);
}