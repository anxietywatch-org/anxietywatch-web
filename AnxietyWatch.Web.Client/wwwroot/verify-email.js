window.anxietyWatch = window.anxietyWatch || {};

window.anxietyWatch.emailVerification = {
    consumeToken() {
        const url = new URL(window.location.href);
        const fragment = new URLSearchParams(url.hash.startsWith("#") ? url.hash.slice(1) : url.hash);
        const token = fragment.get("token") || url.searchParams.get("token") || "";

        url.hash = "";
        url.searchParams.delete("token");
        window.history.replaceState(null, "", `${url.pathname}${url.search}${url.hash}`);

        return token;
    }
};
