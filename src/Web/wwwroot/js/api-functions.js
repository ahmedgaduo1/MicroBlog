// API interaction functions for MicroBlog

/**
 * Like a post
 */
function likePost(postId, button) {
    fetch(`/api/PostApi/like/${postId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Update UI
            button.classList.remove('like-btn', 'text-secondary');
            button.classList.add('unlike-btn', 'text-danger');
            button.innerHTML = '<i class="bi bi-heart-fill"></i> Unlike';
            
            // Update like count
            const likeCountElement = document.querySelector(`.likes-count[data-post-id="${postId}"]`);
            const currentCount = parseInt(likeCountElement.textContent);
            likeCountElement.textContent = `${currentCount + 1} likes`;
            
            // Update event listener
            button.removeEventListener('click', null);
            button.addEventListener('click', function() {
                unlikePost(postId, this);
            });
        }
    })
    .catch(error => {
        console.error('Error liking post:', error);
    });
}

/**
 * Unlike a post
 */
function unlikePost(postId, button) {
    fetch(`/api/PostApi/unlike/${postId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Update UI
            button.classList.remove('unlike-btn', 'text-danger');
            button.classList.add('like-btn', 'text-secondary');
            button.innerHTML = '<i class="bi bi-heart"></i> Like';
            
            // Update like count
            const likeCountElement = document.querySelector(`.likes-count[data-post-id="${postId}"]`);
            const currentCount = parseInt(likeCountElement.textContent);
            likeCountElement.textContent = `${Math.max(0, currentCount - 1)} likes`;
            
            // Update event listener
            button.removeEventListener('click', null);
            button.addEventListener('click', function() {
                likePost(postId, this);
            });
        }
    })
    .catch(error => {
        console.error('Error unliking post:', error);
    });
}

/**
 * Load comments for a post
 */
function loadComments(postId) {
    const commentsContainer = document.querySelector(`.comments-list[data-post-id="${postId}"]`);
    const loadingElement = document.getElementById(`comments-loading-${postId}`);
    
    // Show loading indicator
    if (loadingElement) {
        loadingElement.style.display = 'block';
    }
    
    fetch(`/api/CommentApi/post/${postId}`)
    .then(response => response.json())
    .then(data => {
        // Hide loading indicator
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }
        
        if (data.success && data.comments) {
            // Clear previous comments except loading element
            Array.from(commentsContainer.children).forEach(child => {
                if (child.id !== `comments-loading-${postId}`) {
                    child.remove();
                }
            });
            
            // Render comments
            if (data.comments.length === 0) {
                const noCommentsElement = document.createElement('div');
                noCommentsElement.className = 'text-muted text-center';
                noCommentsElement.textContent = 'No comments yet';
                commentsContainer.appendChild(noCommentsElement);
            } else {
                data.comments.forEach(comment => {
                    const commentElement = createCommentElement(comment);
                    commentsContainer.appendChild(commentElement);
                });
            }
        }
    })
    .catch(error => {
        console.error('Error loading comments:', error);
        // Hide loading indicator and show error
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }
        
        const errorElement = document.createElement('div');
        errorElement.className = 'alert alert-danger';
        errorElement.textContent = 'Failed to load comments.';
        commentsContainer.appendChild(errorElement);
    });
}

/**
 * Create a comment element from comment data
 */
function createCommentElement(comment) {
    const commentElement = document.createElement('div');
    commentElement.className = 'comment mb-3';
    commentElement.setAttribute('data-comment-id', comment.id);
    
    const header = document.createElement('div');
    header.className = 'd-flex align-items-center mb-1';
    
    const userImage = document.createElement('img');
    userImage.src = comment.userProfileImageUrl || '/images/default-avatar.jpg';
    userImage.className = 'rounded-circle me-2';
    userImage.style.width = '30px';
    userImage.style.height = '30px';
    header.appendChild(userImage);
    
    const userInfo = document.createElement('div');
    userInfo.className = 'd-flex flex-column';
    
    const userName = document.createElement('span');
    userName.className = 'fw-bold';
    userName.textContent = comment.userName;
    userInfo.appendChild(userName);
    
    const timestamp = document.createElement('small');
    timestamp.className = 'text-muted';
    timestamp.textContent = formatDate(comment.createdAt);
    userInfo.appendChild(timestamp);
    
    header.appendChild(userInfo);
    commentElement.appendChild(header);
    
    const commentText = document.createElement('p');
    commentText.className = 'mb-1 ms-4 ps-1';
    commentText.textContent = comment.text;
    commentElement.appendChild(commentText);
    
    // Only show delete button if current user is the comment author
    if (comment.userId === getCurrentUserId()) {
        const deleteButton = document.createElement('button');
        deleteButton.className = 'btn btn-sm text-danger ms-4 ps-1';
        deleteButton.innerHTML = '<i class="bi bi-trash"></i> Delete';
        deleteButton.addEventListener('click', function() {
            deleteComment(comment.id);
        });
        commentElement.appendChild(deleteButton);
    }
    
    return commentElement;
}

/**
 * Add a comment to a post
 */
function addComment(postId, text, inputElement) {
    fetch('/api/CommentApi', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            postId: parseInt(postId),
            text: text
        })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success && data.comment) {
            // Clear input
            inputElement.value = '';
            
            // Add new comment to list
            const commentsContainer = document.querySelector(`.comments-list[data-post-id="${postId}"]`);
            const noCommentsElement = commentsContainer.querySelector('.text-muted.text-center');
            
            if (noCommentsElement) {
                noCommentsElement.remove();
            }
            
            const commentElement = createCommentElement(data.comment);
            commentsContainer.insertBefore(commentElement, commentsContainer.firstChild);
            
            // Update comment count
            const commentButton = document.querySelector(`.comment-btn[data-post-id="${postId}"]`);
            const commentText = commentButton.textContent;
            const commentCount = parseInt(commentText.match(/\d+/) || 0) + 1;
            commentButton.innerHTML = `<i class="bi bi-chat-dots"></i> Comment (${commentCount})`;
        }
    })
    .catch(error => {
        console.error('Error adding comment:', error);
    });
}

/**
 * Delete a comment
 */
function deleteComment(commentId) {
    if (confirm('Are you sure you want to delete this comment?')) {
        fetch(`/api/CommentApi/${commentId}`, {
            method: 'DELETE'
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                const commentElement = document.querySelector(`.comment[data-comment-id="${commentId}"]`);
                const postId = commentElement.closest('.comment-section').id.replace('comments-', '');
                
                // Remove comment element
                commentElement.remove();
                
                // Update comment count
                const commentButton = document.querySelector(`.comment-btn[data-post-id="${postId}"]`);
                const commentText = commentButton.textContent;
                const commentCount = Math.max(0, parseInt(commentText.match(/\d+/) || 0) - 1);
                commentButton.innerHTML = `<i class="bi bi-chat-dots"></i> Comment (${commentCount})`;
                
                // If no more comments, show 'no comments' message
                const commentsContainer = document.querySelector(`.comments-list[data-post-id="${postId}"]`);
                if (commentsContainer.children.length === 1 && commentsContainer.children[0].id === `comments-loading-${postId}`) {
                    const noCommentsElement = document.createElement('div');
                    noCommentsElement.className = 'text-muted text-center';
                    noCommentsElement.textContent = 'No comments yet';
                    commentsContainer.appendChild(noCommentsElement);
                }
            }
        })
        .catch(error => {
            console.error('Error deleting comment:', error);
        });
    }
}

/**
 * Format a date for display
 */
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleString();
}

/**
 * Get the current user ID
 */
function getCurrentUserId() {
    // This would typically be set in a data attribute or from a global variable
    // For demonstration purposes, we're returning a placeholder
    // In a real application, this should be properly implemented
    return document.body.getAttribute('data-user-id') || '';
}
