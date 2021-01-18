function ScrollToBottom(id) {
    var textarea = document.getElementById(id);
    textarea.scrollTop = textarea.scrollHeight;
}

function ShowNotLogin(message) {
    alert(message);
}

var term;
function StartTerm(id) {
    term = new Terminal();
    term.open(document.getElementById(id));
}

function WriteToTerm(content) {
    if (term != null) {
        term.write(content);
    }
}
