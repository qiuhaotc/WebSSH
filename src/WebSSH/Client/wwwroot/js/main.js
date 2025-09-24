function ScrollToBottom(id) {
    var textarea = document.getElementById(id);
    textarea.scrollTop = textarea.scrollHeight;
}

function ShowNotLogin(message) {
    alert(message);
}

// Terminal.applyAddon(fit);
// var term = new Terminal();

// function StartTerm(id) {
//     term.open(document.getElementById(id));
//     term.fit();
// }

// function WriteToTerm(content) {
//     if (term !== null) {
//         term.write(content);
//     }
// }

// function ClearTerm() {
//     if (term !== null) {
//         term.reset();
//     }
// }

// File download functionality
window.downloadFile = (filename, base64Content) => {
    const link = document.createElement('a');
    link.download = filename;
    link.href = `data:application/octet-stream;base64,${base64Content}`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
