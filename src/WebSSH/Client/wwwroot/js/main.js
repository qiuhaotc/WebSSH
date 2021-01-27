function ScrollToBottom(id) {
    var textarea = document.getElementById(id);
    textarea.scrollTop = textarea.scrollHeight;
}

function ShowNotLogin(message) {
    alert(message);
}

Terminal.applyAddon(fit);
var term = new Terminal();

function StartTerm(id) {
    term.open(document.getElementById(id));
    term.fit();
}

function WriteToTerm(content) {
    if (term !== null) {
        term.write(content);
    }
}

function ClearTerm() {
    if (term !== null) {
        term.reset();
    }
}
