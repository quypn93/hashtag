// Debug script to inspect TikTok Creative Center UI
// Run this in browser console at: https://ads.tiktok.com/business/creativecenter/inspiration/popular/hashtag/pc/en?countryCode=VN&period=7

console.log('=== TikTok Creative Center UI Inspector ===');

// 1. Find all filter/dropdown elements
console.log('\n1. Looking for filter dropdowns...');
const filterElements = document.querySelectorAll('[class*="filter"], [class*="select"], [class*="dropdown"]');
console.log(`Found ${filterElements.length} potential filter elements:`);
filterElements.forEach((el, i) => {
    if (i < 10) { // Only log first 10
        console.log(`  ${i + 1}. Tag: ${el.tagName}, Classes: ${el.className}, Text: ${el.textContent.substring(0, 50)}`);
    }
});

// 2. Find Industry filter specifically
console.log('\n2. Looking for "Industry" text...');
const industryElements = Array.from(document.querySelectorAll('*')).filter(el =>
    el.textContent.includes('Industry') && el.textContent.length < 100
);
console.log(`Found ${industryElements.length} elements with "Industry" text:`);
industryElements.forEach((el, i) => {
    if (i < 5) {
        console.log(`  ${i + 1}. Tag: ${el.tagName}, Classes: ${el.className}, Text: ${el.textContent.trim()}`);
        console.log(`     Parent: ${el.parentElement?.tagName}, Parent Classes: ${el.parentElement?.className}`);
    }
});

// 3. Find all clickable elements near Industry
console.log('\n3. Looking for clickable elements (buttons, divs with onClick)...');
const clickableElements = document.querySelectorAll('button, [role="button"], [onclick], div[class*="select"]');
console.log(`Found ${clickableElements.length} clickable elements`);

// 4. Check for select/dropdown components
console.log('\n4. Looking for select/dropdown components...');
const selectComponents = document.querySelectorAll('[class*="byted-select"], [class*="Select"], [role="combobox"], [role="listbox"]');
console.log(`Found ${selectComponents.length} select components:`);
selectComponents.forEach((el, i) => {
    if (i < 5) {
        console.log(`  ${i + 1}. Tag: ${el.tagName}, Classes: ${el.className}, Role: ${el.getAttribute('role')}`);
    }
});

// 5. Try to find the actual Industry filter dropdown
console.log('\n5. Trying specific selectors...');
const selectors = [
    '[aria-label*="Industry"]',
    'button:has-text("Industry")',
    '[data-testid*="industry"]',
    '[placeholder*="Industry"]',
    'div:has-text("Industry") button',
    'div:has-text("Industry") [class*="select"]',
];

selectors.forEach(selector => {
    try {
        const el = document.querySelector(selector);
        console.log(`  ${selector}: ${el ? '✓ FOUND' : '✗ Not found'}`);
        if (el) {
            console.log(`    Tag: ${el.tagName}, Classes: ${el.className}`);
        }
    } catch (e) {
        console.log(`  ${selector}: Error - ${e.message}`);
    }
});

// 6. Get page structure around "Industry"
console.log('\n6. Page structure around Industry filter...');
const industryParent = Array.from(document.querySelectorAll('*')).find(el =>
    el.textContent.includes('Industry') && el.children.length > 0 && el.textContent.length < 100
);
if (industryParent) {
    console.log('Industry parent element found:');
    console.log(`  Tag: ${industryParent.tagName}, Classes: ${industryParent.className}`);
    console.log('  Children:');
    Array.from(industryParent.children).forEach((child, i) => {
        console.log(`    ${i + 1}. Tag: ${child.tagName}, Classes: ${child.className}, Text: ${child.textContent.substring(0, 30)}`);
    });
}

console.log('\n=== Inspection Complete ===');
console.log('Copy the results above to help debug the crawler.');
