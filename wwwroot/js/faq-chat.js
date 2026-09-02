document.addEventListener("DOMContentLoaded", () => {
    const widget = document.getElementById("faqChatWidget");
    const chatPanel = document.getElementById("faqChatPanel");
    const toggleButton = document.getElementById("faqToggleButton");
    const messages = document.getElementById("faqMessages");
    const resetButton = document.getElementById("faqResetButton");
    const input = document.getElementById("faqInput");
    const sendButton = document.getElementById("faqSendButton");

    if (!widget || !chatPanel || !toggleButton || !messages ||
        !resetButton || !input || !sendButton) {
        return;
    }

    const avatarUrl = "https://www.gstatic.com/images/branding/product/2x/googleg_48dp.png";
    const errorMessage = "客服目前暫時無法回覆，請稍後再試。";
    const welcomeMessage = "你好，我是旅宿小助手！\n可以協助你了解訂房、入住、退房及訂單查詢等常見問題。";
    const suggestedQuestions = [
        "入住與退房時間是幾點？",
        "最晚什麼時候可以取消訂單？",
        "我要怎麼查詢訂單？"
    ];

    let isSending = false;
    let activeRequestController = null;

    function scrollToBottom() {
        messages.scrollTop = messages.scrollHeight;
    }

    function addMessage(text, sender) {
        const messageRow = document.createElement("div");
        const messageBubble = document.createElement("div");

        messageRow.classList.add("faq-message-row", `faq-message-row--${sender}`);
        messageBubble.classList.add("faq-message-bubble");
        messageBubble.textContent = text;

        if (sender === "bot") {
            const avatar = document.createElement("img");
            avatar.classList.add("faq-message-avatar");
            avatar.src = avatarUrl;
            avatar.alt = "";
            avatar.setAttribute("aria-hidden", "true");
            messageRow.appendChild(avatar);
        }

        messageRow.appendChild(messageBubble);
        messages.appendChild(messageRow);
        scrollToBottom();
    }

    function renderWelcomeMessage() {
        addMessage(welcomeMessage, "bot");
    }

    function renderSuggestions() {
        const suggestions = document.createElement("div");
        suggestions.id = "faqSuggestions";
        suggestions.classList.add("faq-suggestions");
        suggestions.setAttribute("aria-label", "推薦問題");

        suggestedQuestions.forEach(question => {
            const suggestionButton = document.createElement("button");
            suggestionButton.classList.add("faq-suggestion");
            suggestionButton.type = "button";
            suggestionButton.textContent = question;
            suggestionButton.addEventListener("click", () => sendMessage(question));
            suggestions.appendChild(suggestionButton);
        });

        messages.appendChild(suggestions);
        scrollToBottom();
    }

    function openChat() {
        widget.classList.add("is-open");
        chatPanel.setAttribute("aria-hidden", "false");
        toggleButton.setAttribute("aria-expanded", "true");
        toggleButton.setAttribute("aria-label", "收合旅宿小助手");
        input.focus();
        scrollToBottom();
    }

    function closeChat() {
        widget.classList.remove("is-open");
        chatPanel.setAttribute("aria-hidden", "true");
        toggleButton.setAttribute("aria-expanded", "false");
        toggleButton.setAttribute("aria-label", "開啟旅宿小助手");
        toggleButton.focus();
    }

    function toggleChat() {
        if (widget.classList.contains("is-open")) {
            closeChat();
        } else {
            openChat();
        }
    }

    function setSendingState(sending) {
        isSending = sending;
        sendButton.disabled = sending;
        sendButton.textContent = sending ? "傳送中" : "送出";
    }

    function resetConversation() {
        if (activeRequestController) {
            activeRequestController.abort();
            activeRequestController = null;
        }

        setSendingState(false);
        input.value = "";
        messages.replaceChildren();
        renderWelcomeMessage();
        renderSuggestions();

        if (widget.classList.contains("is-open")) {
            input.focus();
        }
    }

    async function sendMessage(suggestedMessage) {
        if (isSending) {
            return;
        }

        const message = (typeof suggestedMessage === "string"
            ? suggestedMessage
            : input.value).trim();

        if (!message) {
            return;
        }

        addMessage(message, "user");
        document.getElementById("faqSuggestions")?.remove();
        input.value = "";

        const requestController = new AbortController();
        activeRequestController = requestController;
        setSendingState(true);

        try {
            const response = await fetch("/Faq/Ask", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ message }),
                signal: requestController.signal
            });

            if (!response.ok) {
                throw new Error(`FAQ request failed with status ${response.status}.`);
            }

            const data = await response.json();
            const reply = typeof data?.reply === "string" ? data.reply.trim() : "";

            if (data?.success !== true || !reply) {
                throw new Error("FAQ response did not contain a reply.");
            }

            if (activeRequestController === requestController) {
                addMessage(reply, "bot");
            }
        } catch (error) {
            if (error.name !== "AbortError" && activeRequestController === requestController) {
                addMessage(errorMessage, "bot");
            }
        } finally {
            if (activeRequestController === requestController) {
                activeRequestController = null;
                setSendingState(false);
                input.focus();
            }
        }
    }

    toggleButton.addEventListener("click", toggleChat);
    resetButton.addEventListener("click", resetConversation);
    sendButton.addEventListener("click", () => sendMessage());

    input.addEventListener("keydown", event => {
        if (event.key === "Enter" && !event.isComposing) {
            event.preventDefault();
            sendMessage();
        }
    });

    resetConversation();
});
