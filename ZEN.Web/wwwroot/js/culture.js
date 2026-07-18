window.blazorGetCultureCookie = function() {
    const cookies = document.cookie.split(';');
    for (let i = 0; i < cookies.length; i++) {
        const cookie = cookies[i].trim();
        if (cookie.startsWith('.AspNetCore.Culture=')) {
            const value = decodeURIComponent(cookie.substring('.AspNetCore.Culture='.length));
            const match = value.match(/culture=([^&|]+)/);
            return match ? match[1] : 'en';
        }
    }
    return 'en';
};

window.blazorSetCultureCookie = function(cultureValue) {
    document.cookie = '.AspNetCore.Culture=' + encodeURIComponent(cultureValue) + ';path=/;max-age=31536000';
};

window.downloadFile = function(fileName, content) {
    const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', fileName);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};