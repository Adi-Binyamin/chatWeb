const chatList = document.getElementById("chatList");
const chatHeader = document.getElementById("chatHeader");
const messagesContainer = document.getElementById("messages");
const messageInput = document.getElementById("messageInput");
const sendButton = document.getElementById("sendButton");
const friendsButton = document.getElementById("friendsButton");

const username = localStorage.getItem("username");

let selectedChatId = null;

friendsButton.addEventListener("click", openFriendSearchWindow);

// SignalR 
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();

connection.on("ReceiveMessage", (message) => {
    if (selectedChatId && message.chatId === selectedChatId) {
        loadMessages(selectedChatId);
    }
    loadChats();
});

connection.start().catch(err => console.error("SignalR Connection Error: ", err));

async function loadChats() {
    if (!username) {
        console.error("Error: No username found in localStorage!");
        if (chatList) {
            chatList.innerHTML = "<p style='color: red;'>Please log in again.</p>";
        }
        return;
    }

    try {
        const response = await fetch(`/chats/${username}`);
        if (!response.ok) {
            throw new Error("Failed to load chats");
        }
        const chats = await response.json();
        
        console.log("Loaded chats from server:", chats);

        chatList.innerHTML = "";
        if (chats.length === 0) {
            chatList.innerHTML = "<p>No chats yet.</p>";
            return;
        }

        chats.forEach(chat => {
            const chatItem = document.createElement("div");
            chatItem.classList.add("chat-item");
            const chatName = document.createElement("span");
            chatName.textContent = chat.otherUser;

            const lastMessageTime = document.createElement("span");
            if (chat.lastMessage) {
                const date = new Date(chat.lastMessage.sentAt);
                lastMessageTime.textContent =
                    date.toLocaleTimeString([], {
                        hour: "2-digit",
                        minute: "2-digit"
                    });
            }
            else {
                lastMessageTime.textContent = "";
            }

            chatItem.appendChild(chatName);
            chatItem.appendChild(lastMessageTime);
            chatItem.addEventListener("click", () => {
                selectChat(chat.chatId, chat.otherUser);
            });
            chatList.appendChild(chatItem);
        });
        
        if (chats.length > 0 && !selectedChatId) {
            selectChat(chats[0].chatId, chats[0].otherUser);
        }
    }
    catch (error) {
        console.error("Error loading chats:", error);
    }
}

async function selectChat(chatId, chatName) {
    selectedChatId = chatId;
    chatHeader.textContent = chatName;
    await loadMessages(chatId);
}

async function loadMessages(chatId) {
    try {
        const response = await fetch(`/chats/${chatId}/messages`);
        if (!response.ok) {
            throw new Error("Failed to load messages");
        }
        const messages = await response.json();
        messagesContainer.innerHTML = "";
        messages.forEach(message => {
            const messageElement = document.createElement("p");
            messageElement.textContent = `${message.username}: ${message.text}`;
            messagesContainer.appendChild(messageElement);
        });
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }
    catch (error) {
        console.error("Error loading messages:", error);
    }
}

sendButton.addEventListener("click", sendMessage);

async function sendMessage() {
    const text = messageInput.value.trim();
    if (!text || !selectedChatId) {
        return;
    }

    try {
        const response = await fetch("/messages", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                chatId: selectedChatId,
                username: username,
                text: text
            })
        });
        if (!response.ok) {
            throw new Error("Failed to send message");
        }

        messageInput.value = "";
    }
    catch (error) {
        console.error("Error sending message:", error);
    }
}

function openFriendSearchWindow() {
    if (document.getElementById("friendPopup")) return;

    const popup = document.createElement('div');
    popup.id = "friendPopup";
    popup.classList.add('friend-popup');

    const title = document.createElement('h2');
    title.classList.add('friend-popup-title');
    title.textContent = 'Find a Friend';

    const searchInput = document.createElement('input');
    searchInput.type = 'text';
    searchInput.placeholder = 'Enter username...';
    searchInput.classList.add('search-user-input');

    const searchButton = document.createElement('button');
    searchButton.textContent = 'Search';
    searchButton.classList.add('search-user-button');

    const resultArea = document.createElement('div');
    resultArea.classList.add('search-result-area');

    const closeButton = document.createElement('button');
    closeButton.textContent = 'Close';
    closeButton.classList.add('close-popup-button');

    searchButton.addEventListener("click", async () => {
        const query = searchInput.value.trim();
        if (!query) return;

        try {
            const response = await fetch(`/users/search?query=${encodeURIComponent(query)}&currentUsername=${encodeURIComponent(username)}`);
            resultArea.innerHTML = "";
            if (!response.ok) {
                const errorMsg = document.createElement('p');
                errorMsg.textContent = "User not found.";
                errorMsg.style.color = "red";
                resultArea.appendChild(errorMsg);
                return;
            }

            const data = await response.json();
            const userText = document.createElement('p');
            userText.textContent = `User: ${data.username}`;
            resultArea.appendChild(userText);

            const actionButton = document.createElement('button');

            if (data.isAlreadyConnected) {
                actionButton.textContent = "Already Connected";
                actionButton.disabled = true;
            } else {
                actionButton.textContent = "Connect";
                actionButton.addEventListener("click", async () => {
                    await createChatWithUser(data.username);
                    popup.remove();
                });
            }

            resultArea.appendChild(actionButton);

        } catch (error) {
            console.error("Error searching user:", error);
        }
    });

    closeButton.addEventListener('click', () => {
        popup.remove();
    });

    popup.appendChild(title);
    popup.appendChild(searchInput);
    popup.appendChild(searchButton);
    popup.appendChild(resultArea);
    popup.appendChild(closeButton);

    document.body.appendChild(popup);
}

async function createChatWithUser(targetUsername) {
    try {
        const response = await fetch(`/chats?username1=${encodeURIComponent(username)}&username2=${encodeURIComponent(targetUsername)}`, {
            method: "POST"
        });

        if (!response.ok) {
            throw new Error("Failed to create chat");
        }

        const newChat = await response.json();
        
        await loadChats();
        
        selectChat(newChat.chatId, targetUsername);
    } catch (error) {
        console.error("Error creating chat:", error);
    }
}

loadChats();