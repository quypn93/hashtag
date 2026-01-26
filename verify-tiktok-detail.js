// INSTRUCTION:
// 1. Go to https://ads.tiktok.com/business/creativecenter/hashtag/seagames33/pc/en?countryCode=VN&period=7
// 2. Open browser console (F12)
// 3. Paste and run this script
// 4. Check the output

console.log('=== TikTok Detail Page Analysis ===\n');

// Get all page text
const bodyText = document.body.innerText;
console.log('Page text length:', bodyText.length, 'characters\n');

// Check for "views" keyword
const viewMatches = bodyText.match(/view[s]?/gi);
console.log('Contains "view(s)":', viewMatches ? viewMatches.length + ' times' : 'NOT FOUND');

// Check for "posts" keyword
const postMatches = bodyText.match(/post[s]?/gi);
console.log('Contains "post(s)":', postMatches ? postMatches.length + ' times' : 'NOT FOUND');

// Check for "videos" keyword
const videoMatches = bodyText.match(/video[s]?/gi);
console.log('Contains "video(s)":', videoMatches ? videoMatches.length + ' times' : 'NOT FOUND\n');

// Look for large numbers (potential view/post counts)
const numberPattern = /\b([0-9]{1,3}(?:,[0-9]{3})+|[0-9]+\.?[0-9]*[KMB])\b/gi;
const numbers = bodyText.match(numberPattern);

if (numbers && numbers.length > 0) {
    console.log('Large numbers found:', numbers.length);
    console.log('First 10 numbers:', numbers.slice(0, 10));
    console.log('');
} else {
    console.log('⚠️ NO large numbers found!\n');
}

// Try to find specific patterns
console.log('=== Pattern Search ===');

// Pattern 1: "Video views" or "Views"
const viewPattern = /(?:video\s+)?views?[:\s]+([0-9,.KMB]+)/gi;
const viewResult = bodyText.match(viewPattern);
console.log('Pattern "views: XXX":', viewResult || 'NOT FOUND');

// Pattern 2: "Posts"
const postPattern = /posts?[:\s]+([0-9,.KMB]+)/gi;
const postResult = bodyText.match(postPattern);
console.log('Pattern "posts: XXX":', postResult || 'NOT FOUND');

// Pattern 3: Numbers near "view"
const nearViewPattern = /(\w+\s+)?([0-9,.KMB]+)\s+(video\s+)?views?/gi;
const nearView = bodyText.match(nearViewPattern);
console.log('Pattern "XXX views":', nearView || 'NOT FOUND');

console.log('\n=== DOM Element Search ===');

// Search for elements with stat-like classes
const statSelectors = [
    '[class*="stat"]',
    '[class*="count"]',
    '[class*="metric"]',
    '[class*="number"]',
    '[class*="value"]',
    '[data-*="view"]',
    '[data-*="post"]'
];

statSelectors.forEach(selector => {
    const elements = document.querySelectorAll(selector);
    if (elements.length > 0) {
        console.log(`Selector "${selector}":`, elements.length, 'elements');
        if (elements.length > 0 && elements.length < 10) {
            Array.from(elements).forEach((el, i) => {
                const text = el.innerText.trim().substring(0, 100);
                if (text) console.log(`  [${i}]:`, text);
            });
        }
    }
});

console.log('\n=== Sample Text Sections ===');

// Split text into sections and show samples
const sections = bodyText.split('\n').filter(line => line.trim().length > 20);
console.log('Text sections:', sections.length);
console.log('\nFirst 10 sections:');
sections.slice(0, 10).forEach((section, i) => {
    console.log(`[${i}]:`, section.substring(0, 100));
});

console.log('\n=== CONCLUSION ===');
if (viewResult || postResult || nearView) {
    console.log('✅ Page likely contains ViewCount/PostCount data');
    console.log('Recommended: Proceed with detail page crawling implementation');
} else if (numbers && numbers.length > 5) {
    console.log('🟡 Page has numbers but no clear view/post labels');
    console.log('Recommended: Need to examine page structure manually');
} else {
    console.log('❌ Page does NOT contain ViewCount/PostCount data');
    console.log('Recommended: Find alternative data source or use rank-based metrics');
}

// Export data for analysis
console.log('\n=== Export Full Text (for regex testing) ===');
console.log('Run this to copy text to clipboard:');
console.log('navigator.clipboard.writeText(document.body.innerText)');
