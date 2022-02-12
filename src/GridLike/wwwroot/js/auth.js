export function SignIn(userName, password, redirect) {
    var request = new XMLHttpRequest();
    request.open("POST", "/api/auth/signin");
    request.setRequestHeader("Content-Type", "application/json");
    request.setRequestHeader("Accept", "application/json");

    request.onreadystatechange = function () {
        if (request.readyState === 4) {
            if (redirect) location.replace(redirect);
        }
    };

    request.send(JSON.stringify({ user: userName, password: password }));
}

export function SignOut(redirect) {
    var request = new XMLHttpRequest();
    request.open("POST", "/api/auth/signout");
    request.setRequestHeader("Content-Type", "application/json");
    request.setRequestHeader("Accept", "application/json");

    request.onreadystatechange = function () {
        if (request.readyState === 4) {
            if (redirect) location.replace(redirect);
        }
    };

    request.send();
}