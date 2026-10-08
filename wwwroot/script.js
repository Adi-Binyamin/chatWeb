const form = document.querySelector('.auth-form');

const userName = document.getElementById('username');
const password = document.getElementById('password');
const logIn_register = document.getElementsByName('action');

form.addEventListener('submit', async function(e) {
    e.preventDefault();

    if (!userName.value || !password.value) {
        return;
    }

    let url;

    if (logIn_register[0].checked) {
        url = 'http://localhost:5054/register';
    } 
    else if (logIn_register[1].checked) {
        url = 'http://localhost:5054/login';
    } 
    else {
        return;
    }

    const response = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            username: userName.value,
            password: password.value
        })
    });

    const data = await response.json();

    if (response.ok) {
        localStorage.setItem("username", userName.value);
        window.location.href = 'chat.html';
    } else {
        alert(data.message);
    }
});
