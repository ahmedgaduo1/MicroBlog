// Site.js - Main JavaScript for MicroBlog

// Wait for the DOM to be fully loaded
document.addEventListener('DOMContentLoaded', function () {
    // Enable all tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    });

    // Enable all popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'))
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl)
    });

    // Add active state to nav links based on current page
    const currentPath = window.location.pathname;
    document.querySelectorAll('.navbar-nav .nav-link').forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });

    // For mobile menu, collapse the menu when a link is clicked
    const navLinks = document.querySelectorAll('.navbar-nav .nav-link');
    const menuToggle = document.getElementById('navbarSupportedContent');
    navLinks.forEach((link) => {
        link.addEventListener('click', () => {
            if (menuToggle && menuToggle.classList.contains('show')) {
                menuToggle.classList.remove('show');
            }
        });
    });
    
    // Like functionality
    document.querySelectorAll('.like-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            const postId = this.getAttribute('data-post-id');
            likePost(postId, this);
        });
    });
    
    // Unlike functionality
    document.querySelectorAll('.unlike-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            const postId = this.getAttribute('data-post-id');
            unlikePost(postId, this);
        });
    });
    
    // Comment section toggle
    document.querySelectorAll('.comment-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            const postId = this.getAttribute('data-post-id');
            const commentSection = document.getElementById(`comments-${postId}`);
            
            if (commentSection.style.display === 'none') {
                commentSection.style.display = 'block';
                loadComments(postId);
            } else {
                commentSection.style.display = 'none';
            }
        });
    });
    
    // Comment form submission
    document.querySelectorAll('.comment-form').forEach(form => {
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            const postId = this.getAttribute('data-post-id');
            const input = this.querySelector('input');
            const commentText = input.value.trim();
            
            if (commentText) {
                addComment(postId, commentText, input);
            }
        });
    });
});
