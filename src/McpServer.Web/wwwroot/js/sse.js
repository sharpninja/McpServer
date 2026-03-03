(() => {
    const subscriptions = new Map();
    const initialBackoffMs = 1000;
    const maxBackoffMs = 30000;

    function cleanupSource(subscription) {
        if (subscription.source) {
            subscription.source.close();
            subscription.source = null;
        }
    }

    function computeBackoffDelay(retryCount) {
        const exponential = initialBackoffMs * Math.pow(2, retryCount);
        const jitter = Math.floor(Math.random() * 500);
        return Math.min(exponential + jitter, maxBackoffMs);
    }

    function notify(subscription, payload) {
        if (!subscription.dotNetRef || typeof subscription.dotNetRef.invokeMethodAsync !== "function") {
            return;
        }

        const callbackMethod = subscription.callbackMethod || "OnSseMessage";
        subscription.dotNetRef
            .invokeMethodAsync(callbackMethod, payload)
            .catch(() => {
                // Skeleton intentionally suppresses callback errors to keep reconnect loop alive.
            });
    }

    function scheduleReconnect(subscription) {
        if (subscription.disposed) {
            return;
        }

        const delay = computeBackoffDelay(subscription.retryCount++);
        subscription.timerId = setTimeout(() => connect(subscription), delay);
    }

    function connect(subscription) {
        if (subscription.disposed) {
            return;
        }

        cleanupSource(subscription);
        const source = new EventSource(subscription.url, { withCredentials: true });
        subscription.source = source;

        source.onopen = () => {
            subscription.retryCount = 0;
        };

        source.onmessage = event => {
            notify(subscription, {
                type: event.type || "message",
                data: event.data,
                lastEventId: event.lastEventId || ""
            });
        };

        source.onerror = () => {
            cleanupSource(subscription);
            scheduleReconnect(subscription);
        };
    }

    function subscribe(subscriptionId, url, dotNetRef, callbackMethod) {
        if (!subscriptionId) {
            throw new Error("subscriptionId is required.");
        }

        if (!url) {
            throw new Error("url is required.");
        }

        dispose(subscriptionId);

        const subscription = {
            id: subscriptionId,
            url,
            dotNetRef: dotNetRef ?? null,
            callbackMethod: callbackMethod ?? "OnSseMessage",
            source: null,
            timerId: null,
            retryCount: 0,
            disposed: false
        };

        subscriptions.set(subscriptionId, subscription);
        connect(subscription);
    }

    function dispose(subscriptionId) {
        const subscription = subscriptions.get(subscriptionId);
        if (!subscription) {
            return;
        }

        subscription.disposed = true;
        if (subscription.timerId) {
            clearTimeout(subscription.timerId);
            subscription.timerId = null;
        }

        cleanupSource(subscription);
        subscriptions.delete(subscriptionId);
    }

    function disposeAll() {
        for (const subscriptionId of subscriptions.keys()) {
            dispose(subscriptionId);
        }
    }

    window.mcpSse = {
        subscribe,
        dispose,
        disposeAll
    };
})();
