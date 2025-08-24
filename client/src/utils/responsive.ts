/**
 * Responsive Design Utilities for Mobile/Tablet/Desktop Support
 * User Story 5.1.1: Accessibility and Performance
 */

// Breakpoint definitions
export const breakpoints = {
  mobile: '480px',
  tablet: '768px',
  desktop: '1024px',
  large: '1200px'
};

// Media query utilities
export const mediaQueries = {
  mobile: `(max-width: ${breakpoints.mobile})`,
  tablet: `(min-width: ${breakpoints.mobile}) and (max-width: ${breakpoints.tablet})`,
  desktop: `(min-width: ${breakpoints.tablet})`,
  large: `(min-width: ${breakpoints.desktop})`
};

// Device detection
export const device = {
  /**
   * Detects current device type
   */
  getType: (): 'mobile' | 'tablet' | 'desktop' => {
    if (window.innerWidth <= 480) return 'mobile';
    if (window.innerWidth <= 768) return 'tablet';
    return 'desktop';
  },

  /**
   * Checks if device is mobile
   */
  isMobile: (): boolean => {
    return window.innerWidth <= 480 || /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
  },

  /**
   * Checks if device is tablet
   */
  isTablet: (): boolean => {
    return window.innerWidth > 480 && window.innerWidth <= 768;
  },

  /**
   * Checks if device is desktop
   */
  isDesktop: (): boolean => {
    return window.innerWidth > 768;
  },

  /**
   * Checks if device supports touch
   */
  isTouch: (): boolean => {
    return 'ontouchstart' in window || navigator.maxTouchPoints > 0;
  },

  /**
   * Gets device pixel ratio for high DPI displays
   */
  getPixelRatio: (): number => {
    return window.devicePixelRatio || 1;
  },

  /**
   * Checks if device is in landscape orientation
   */
  isLandscape: (): boolean => {
    return window.innerWidth > window.innerHeight;
  }
};

// Viewport utilities
export const viewport = {
  /**
   * Gets current viewport dimensions
   */
  getDimensions: () => ({
    width: window.innerWidth,
    height: window.innerHeight
  }),

  /**
   * Checks if element is in viewport
   */
  isInViewport: (element: HTMLElement): boolean => {
    const rect = element.getBoundingClientRect();
    return (
      rect.top >= 0 &&
      rect.left >= 0 &&
      rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
      rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
  },

  /**
   * Smoothly scrolls element into view
   */
  scrollIntoView: (element: HTMLElement, options?: ScrollIntoViewOptions): void => {
    if ('scrollIntoView' in element) {
      element.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
        inline: 'nearest',
        ...options
      });
    }
  },

  /**
   * Sets up viewport meta tag for mobile optimization
   */
  setupMobileViewport: (): void => {
    let viewport = document.querySelector('meta[name="viewport"]') as HTMLMetaElement;
    
    if (!viewport) {
      viewport = document.createElement('meta');
      viewport.name = 'viewport';
      document.head.appendChild(viewport);
    }
    
    viewport.content = 'width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=yes';
  }
};

// Responsive image utilities
export const responsiveImage = {
  /**
   * Creates responsive image attributes
   */
  createAttributes: (
    src: string,
    sizes: { width: number; density?: number }[]
  ) => {
    const srcSet = sizes.map(size => {
      const density = size.density || 1;
      return `${src}?w=${size.width}&dpr=${density} ${size.width * density}w`;
    }).join(', ');
    
    const sizesAttr = sizes.map((size, index) => {
      if (index === sizes.length - 1) return `${size.width}px`;
      return `(max-width: ${size.width}px) ${size.width}px`;
    }).join(', ');
    
    return {
      src: `${src}?w=${sizes[0].width}`,
      srcSet,
      sizes: sizesAttr
    };
  },

  /**
   * Lazy loads images with intersection observer
   */
  setupLazyLoading: (selector: string = 'img[data-src]'): void => {
    if ('IntersectionObserver' in window) {
      const imageObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            const img = entry.target as HTMLImageElement;
            const src = img.dataset.src;
            
            if (src) {
              img.src = src;
              img.removeAttribute('data-src');
              imageObserver.unobserve(img);
            }
          }
        });
      });
      
      document.querySelectorAll(selector).forEach(img => {
        imageObserver.observe(img);
      });
    } else {
      // Fallback for older browsers
      document.querySelectorAll(selector).forEach(img => {
        const imgElement = img as HTMLImageElement;
        const src = imgElement.dataset.src;
        if (src) {
          imgElement.src = src;
          imgElement.removeAttribute('data-src');
        }
      });
    }
  }
};

// Touch interaction utilities
export const touch = {
  /**
   * Sets up touch-friendly interactions
   */
  setupTouchInteractions: (container: HTMLElement): void => {
    // Increase touch target sizes
    const touchTargets = container.querySelectorAll('button, a, input, [role="button"]');
    touchTargets.forEach(target => {
      const element = target as HTMLElement;
      const styles = window.getComputedStyle(element);
      const minSize = 44; // 44px minimum touch target
      
      if (parseInt(styles.height) < minSize) {
        element.style.minHeight = `${minSize}px`;
      }
      if (parseInt(styles.width) < minSize) {
        element.style.minWidth = `${minSize}px`;
      }
      
      // Add touch feedback
      element.addEventListener('touchstart', () => {
        element.classList.add('touch-active');
      });
      
      element.addEventListener('touchend', () => {
        setTimeout(() => {
          element.classList.remove('touch-active');
        }, 150);
      });
    });
    
    // Improve scrolling performance
    (container.style as any).webkitOverflowScrolling = 'touch';
  },

  /**
   * Handles swipe gestures
   */
  setupSwipeGestures: (
    element: HTMLElement,
    callbacks: {
      onSwipeLeft?: () => void;
      onSwipeRight?: () => void;
      onSwipeUp?: () => void;
      onSwipeDown?: () => void;
    }
  ): () => void => {
    let startX = 0;
    let startY = 0;
    let endX = 0;
    let endY = 0;
    
    const handleTouchStart = (e: TouchEvent) => {
      startX = e.touches[0].clientX;
      startY = e.touches[0].clientY;
    };
    
    const handleTouchEnd = (e: TouchEvent) => {
      endX = e.changedTouches[0].clientX;
      endY = e.changedTouches[0].clientY;
      
      const deltaX = endX - startX;
      const deltaY = endY - startY;
      const minSwipeDistance = 50;
      
      if (Math.abs(deltaX) > Math.abs(deltaY)) {
        // Horizontal swipe
        if (Math.abs(deltaX) > minSwipeDistance) {
          if (deltaX > 0 && callbacks.onSwipeRight) {
            callbacks.onSwipeRight();
          } else if (deltaX < 0 && callbacks.onSwipeLeft) {
            callbacks.onSwipeLeft();
          }
        }
      } else {
        // Vertical swipe
        if (Math.abs(deltaY) > minSwipeDistance) {
          if (deltaY > 0 && callbacks.onSwipeDown) {
            callbacks.onSwipeDown();
          } else if (deltaY < 0 && callbacks.onSwipeUp) {
            callbacks.onSwipeUp();
          }
        }
      }
    };
    
    element.addEventListener('touchstart', handleTouchStart, { passive: true });
    element.addEventListener('touchend', handleTouchEnd, { passive: true });
    
    // Return cleanup function
    return () => {
      element.removeEventListener('touchstart', handleTouchStart);
      element.removeEventListener('touchend', handleTouchEnd);
    };
  }
};

// Responsive layout utilities
export const layout = {
  /**
   * Creates responsive grid system
   */
  createGrid: (columns: { mobile: number; tablet: number; desktop: number }) => ({
    display: 'grid',
    gridTemplateColumns: `repeat(${columns.mobile}, 1fr)`,
    gap: '1rem',
    [`@media ${mediaQueries.tablet}`]: {
      gridTemplateColumns: `repeat(${columns.tablet}, 1fr)`
    },
    [`@media ${mediaQueries.desktop}`]: {
      gridTemplateColumns: `repeat(${columns.desktop}, 1fr)`
    }
  }),

  /**
   * Creates responsive spacing
   */
  createSpacing: (sizes: { mobile: string; tablet?: string; desktop?: string }) => ({
    padding: sizes.mobile,
    [`@media ${mediaQueries.tablet}`]: {
      padding: sizes.tablet || sizes.mobile
    },
    [`@media ${mediaQueries.desktop}`]: {
      padding: sizes.desktop || sizes.tablet || sizes.mobile
    }
  }),

  /**
   * Creates responsive typography
   */
  createTypography: (sizes: { mobile: string; tablet?: string; desktop?: string }) => ({
    fontSize: sizes.mobile,
    [`@media ${mediaQueries.tablet}`]: {
      fontSize: sizes.tablet || sizes.mobile
    },
    [`@media ${mediaQueries.desktop}`]: {
      fontSize: sizes.desktop || sizes.tablet || sizes.mobile
    }
  })
};

// Responsive testing utilities
export const testing = {
  /**
   * Tests responsive design at different viewports
   */
  testViewports: (viewports: { width: number; height: number; name: string }[]) => {
    viewports.forEach(viewport => {
      console.log(`Testing ${viewport.name} (${viewport.width}x${viewport.height})`);
      
      // Set viewport size (for testing purposes)
      if (window.visualViewport) {
        Object.defineProperty(window, 'innerWidth', { value: viewport.width });
        Object.defineProperty(window, 'innerHeight', { value: viewport.height });
      }
      
      // Check if layout adapts properly
      const issues: string[] = [];
      
      // Check for horizontal overflow
      const body = document.body;
      if (body.scrollWidth > viewport.width) {
        issues.push('Horizontal overflow detected');
      }
      
      // Check touch target sizes on mobile
      if (viewport.width <= 480) {
        const touchTargets = document.querySelectorAll('button, a, input, [role="button"]');
        touchTargets.forEach(target => {
          const rect = target.getBoundingClientRect();
          if (rect.width < 44 || rect.height < 44) {
            issues.push(`Touch target too small: ${target.tagName}`);
          }
        });
      }
      
      if (issues.length > 0) {
        console.warn(`${viewport.name} issues:`, issues);
      } else {
        console.log(`${viewport.name} passed all checks`);
      }
    });
  },

  /**
   * Tests responsive images
   */
  testResponsiveImages: (): void => {
    const images = document.querySelectorAll('img');
    const issues: string[] = [];
    
    images.forEach(img => {
      if (!img.srcset && !img.sizes) {
        issues.push(`Image not responsive: ${img.src}`);
      }
      
      if (img.loading !== 'lazy' && !viewport.isInViewport(img)) {
        issues.push(`Image not lazy loaded: ${img.src}`);
      }
    });
    
    if (issues.length > 0) {
      console.warn('Responsive image issues:', issues);
    } else {
      console.log('All images are responsive');
    }
  }
};

// Orientation change handling
export const orientation = {
  /**
   * Handles orientation changes
   */
  setupOrientationHandling: (callback?: (orientation: 'portrait' | 'landscape') => void): () => void => {
    const handleOrientationChange = () => {
      const orientation = window.innerWidth > window.innerHeight ? 'landscape' : 'portrait';
      
      // Update CSS custom property
      document.documentElement.style.setProperty('--viewport-height', `${window.innerHeight}px`);
      
      if (callback) {
        callback(orientation);
      }
    };
    
    window.addEventListener('orientationchange', handleOrientationChange);
    window.addEventListener('resize', handleOrientationChange);
    
    // Initial call
    handleOrientationChange();
    
    // Return cleanup function
    return () => {
      window.removeEventListener('orientationchange', handleOrientationChange);
      window.removeEventListener('resize', handleOrientationChange);
    };
  }
};

export default {
  breakpoints,
  mediaQueries,
  device,
  viewport,
  responsiveImage,
  touch,
  layout,
  testing,
  orientation
};
