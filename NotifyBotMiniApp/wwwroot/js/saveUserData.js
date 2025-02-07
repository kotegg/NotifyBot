document.addEventListener("DOMContentLoaded", () => {
    let telegram = window.Telegram.WebApp;

    if (!telegram.initDataUnsafe || !telegram.initDataUnsafe.user) {
        console.error("Telegram data is unavailable. Make sure to open this page from Telegram.");
        return;
    }

    const chatId = telegram.initDataUnsafe.user.id;
    const username = telegram.initDataUnsafe.user.username;
    const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;

    fetch('/Base/SaveUserData', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ chatId, username, timeZone }),
    })
    .then(response => {
        if (response.ok) {
            console.log("User data saved successfully.");
        } else {
            console.error("Failed to save user data.");
        }
    })
    .catch(error => console.error("Error:", error));
});
