document.addEventListener("DOMContentLoaded", () => {
    const input = document.getElementById("faqInput");
    const sendButton = document.getElementById("faqSendButton");
    const messages = document.getElementById("faqMessages");
    const toggleButton = document.getElementById("faqToggleButton");
    const closeButton = document.getElementById("faqCloseButton");
    const chatPanel = document.getElementById("faqChatPanel");

    if (
        !input ||
        !sendButton ||
        !messages ||
        !toggleButton ||
        !closeButton ||
        !chatPanel
    ) {
        return;
    }

    function addMessage(text, sender) {
        const messageElement = document.createElement("div");

        messageElement.classList.add("faq-message", sender);
        messageElement.textContent = text;

        messages.appendChild(messageElement);
    }

    function openChat() {
        chatPanel.hidden = false;
        toggleButton.setAttribute("aria-expanded", "true");
        input.focus();
    }

    function closeChat() {
        chatPanel.hidden = true;
        toggleButton.setAttribute("aria-expanded", "false");
    }

    async function sendMessage() {
        const message = input.value.trim();

        if (!message) {
            return;
        }

        addMessage(message, "user");
        input.value = "";
        sendButton.disabled = true;

        try {
            const response = await fetch("/Faq/Ask", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    message: message
                })
            });

            const data = await response.json();

            addMessage(
                data.reply || "客服目前暫時無法回覆，請稍後再試。",
                "bot"
            );
        } catch (error) {
            addMessage(
                "客服目前暫時無法回覆，請稍後再試。",
                "bot"
            );
        } finally {
            sendButton.disabled = false;
            input.focus();
        }
    }

    sendButton.addEventListener("click", sendMessage);
    toggleButton.addEventListener("click", openChat);
    closeButton.addEventListener("click", closeChat);

    input.addEventListener("keydown", event => {
        if (event.key === "Enter") {
            sendMessage();
        }
    });
});