// Paste this code into Chrome DevTools Console (F12 → Console tab)
// while on https://ads.tiktok.com page

(function() {
    // Get all cookies for the current domain
    const cookies = document.cookie.split(';').map(c => c.trim());

    // Also get cookies via document.cookie API
    const cookieArray = [];

    cookies.forEach(cookie => {
        const [name, value] = cookie.split('=');
        if (name && value) {
            cookieArray.push({
                name: name.trim(),
                value: value.trim(),
                domain: '.tiktok.com',
                path: '/',
                expires: Math.floor(Date.now() / 1000) + (365 * 24 * 60 * 60), // 1 year
                httpOnly: false,
                secure: true,
                sameSite: 'None'
            });
        }
    });

    // Output as JSON string (ready to paste into appsettings.json)
    const jsonString = JSON.stringify(cookieArray);

    console.log('=== COPY THIS JSON ===');
    console.log(jsonString);
    console.log('=== END ===');

    // Also copy to clipboard
    navigator.clipboard.writeText(jsonString).then(() => {
        console.log('✅ Cookies copied to clipboard!');
        console.log('📋 Paste this into appsettings.json → CrawlerSettings.TikTokCookies');
    }).catch(err => {
        console.error('❌ Failed to copy to clipboard:', err);
    });

    return cookieArray;
})();
