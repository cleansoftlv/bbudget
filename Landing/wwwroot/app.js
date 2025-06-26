// Get the lightbox elements
const lightbox = document.getElementById('lightbox');
const lightboxImg = document.getElementById('lightbox-img');
const lightboxCaption = document.getElementById('lightbox-caption');
const closeBtn = document.getElementsByClassName('close-lightbox')[0];
 
// Add click event to all images
document.addEventListener('DOMContentLoaded', function () {
    const images = document.querySelectorAll('.feature-card img, .mobile-screenshot img');

    images.forEach(img => {
        img.addEventListener('click', function () {
            lightbox.style.display = 'block';
            lightboxImg.src = this.src;
            lightboxCaption.textContent = this.alt || '';
        });
    });
});

// Close lightbox when clicking the X
closeBtn.addEventListener('click', function () {
    lightbox.style.display = 'none';
});

// Close lightbox when clicking outside the image
lightbox.addEventListener('click', function (event) {
    if (event.target === lightbox) {
        lightbox.style.display = 'none';
    }
});

// Close lightbox with Escape key
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape' && lightbox.style.display === 'block') {
        lightbox.style.display = 'none';
    }
});