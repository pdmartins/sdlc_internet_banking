/**
 * Accessibility Utilities for WCAG 2.1 AA Compliance
 * User Story 5.1.1: Accessibility and Performance
 */

// Focus management
export const manageFocus = {
  /**
   * Sets focus to an element by ID or selector
   */
  setFocus: (selector: string): void => {
    setTimeout(() => {
      const element = document.querySelector(selector) as HTMLElement;
      if (element) {
        element.focus();
      }
    }, 100);
  },

  /**
   * Manages focus for modal dialogs
   */
  trapFocus: (container: HTMLElement): (() => void) => {
    const focusableElements = container.querySelectorAll(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    ) as NodeListOf<HTMLElement>;

    const firstElement = focusableElements[0];
    const lastElement = focusableElements[focusableElements.length - 1];

    const handleTabKey = (e: KeyboardEvent) => {
      if (e.key === 'Tab') {
        if (e.shiftKey) {
          if (document.activeElement === firstElement) {
            lastElement.focus();
            e.preventDefault();
          }
        } else {
          if (document.activeElement === lastElement) {
            firstElement.focus();
            e.preventDefault();
          }
        }
      }
    };

    const handleEscapeKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        const closeButton = container.querySelector('[aria-label*="Fechar"]') as HTMLElement;
        if (closeButton) {
          closeButton.click();
        }
      }
    };

    document.addEventListener('keydown', handleTabKey);
    document.addEventListener('keydown', handleEscapeKey);

    // Set initial focus
    if (firstElement) {
      firstElement.focus();
    }

    // Return cleanup function
    return () => {
      document.removeEventListener('keydown', handleTabKey);
      document.removeEventListener('keydown', handleEscapeKey);
    };
  },

  /**
   * Announces messages to screen readers
   */
  announce: (message: string, priority: 'polite' | 'assertive' = 'polite'): void => {
    const announcer = document.createElement('div');
    announcer.setAttribute('aria-live', priority);
    announcer.setAttribute('aria-atomic', 'true');
    announcer.className = 'sr-only';
    announcer.textContent = message;
    
    document.body.appendChild(announcer);
    
    setTimeout(() => {
      document.body.removeChild(announcer);
    }, 1000);
  }
};

// ARIA utilities
export const aria = {
  /**
   * Generates unique IDs for aria-describedby relationships
   */
  generateId: (prefix: string = 'aria'): string => {
    return `${prefix}-${Math.random().toString(36).substr(2, 9)}`;
  },

  /**
   * Sets up form field ARIA relationships
   */
  setupFieldAria: (fieldId: string, errorId?: string, helpId?: string): Record<string, string> => {
    const ariaAttributes: Record<string, string> = {};
    
    if (errorId || helpId) {
      const describedBy = [errorId, helpId].filter(Boolean).join(' ');
      ariaAttributes['aria-describedby'] = describedBy;
    }
    
    return ariaAttributes;
  },

  /**
   * Creates ARIA attributes for error states
   */
  errorState: (hasError: boolean, errorId?: string): Record<string, string | boolean> => {
    const attributes: Record<string, string | boolean> = {
      'aria-invalid': hasError
    };
    
    if (hasError && errorId) {
      attributes['aria-describedby'] = errorId;
    }
    
    return attributes;
  }
};

// Keyboard navigation utilities
export const keyboard = {
  /**
   * Handles Enter and Space key activation for custom buttons
   */
  handleActivation: (callback: () => void) => (e: KeyboardEvent): void => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      callback();
    }
  },

  /**
   * Handles arrow key navigation in lists or grids
   */
  handleArrowNavigation: (
    container: HTMLElement,
    options: {
      direction: 'horizontal' | 'vertical' | 'grid';
      wrap?: boolean;
      itemSelector?: string;
    }
  ) => (e: KeyboardEvent): void => {
    const { direction, wrap = true, itemSelector = '[role="button"], button, [tabindex="0"]' } = options;
    const items = Array.from(container.querySelectorAll(itemSelector)) as HTMLElement[];
    const currentIndex = items.indexOf(document.activeElement as HTMLElement);
    
    if (currentIndex === -1) return;
    
    let nextIndex = currentIndex;
    
    switch (e.key) {
      case 'ArrowUp':
        if (direction === 'vertical' || direction === 'grid') {
          nextIndex = wrap && currentIndex === 0 ? items.length - 1 : Math.max(0, currentIndex - 1);
        }
        break;
      case 'ArrowDown':
        if (direction === 'vertical' || direction === 'grid') {
          nextIndex = wrap && currentIndex === items.length - 1 ? 0 : Math.min(items.length - 1, currentIndex + 1);
        }
        break;
      case 'ArrowLeft':
        if (direction === 'horizontal' || direction === 'grid') {
          nextIndex = wrap && currentIndex === 0 ? items.length - 1 : Math.max(0, currentIndex - 1);
        }
        break;
      case 'ArrowRight':
        if (direction === 'horizontal' || direction === 'grid') {
          nextIndex = wrap && currentIndex === items.length - 1 ? 0 : Math.min(items.length - 1, currentIndex + 1);
        }
        break;
      case 'Home':
        nextIndex = 0;
        break;
      case 'End':
        nextIndex = items.length - 1;
        break;
    }
    
    if (nextIndex !== currentIndex) {
      e.preventDefault();
      items[nextIndex].focus();
    }
  }
};

// Color contrast utilities
export const contrast = {
  /**
   * Checks if color contrast meets WCAG AA standards
   */
  checkContrast: (foreground: string, background: string): boolean => {
    // Simplified contrast check - in production, use a library like chroma-js
    // This is a basic implementation for demonstration
    const getLuminance = (color: string): number => {
      // Basic hex to RGB conversion and luminance calculation
      const hex = color.replace('#', '');
      const r = parseInt(hex.substr(0, 2), 16) / 255;
      const g = parseInt(hex.substr(2, 2), 16) / 255;
      const b = parseInt(hex.substr(4, 2), 16) / 255;
      
      const sRGB = [r, g, b].map(c => 
        c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4)
      );
      
      return 0.2126 * sRGB[0] + 0.7152 * sRGB[1] + 0.0722 * sRGB[2];
    };
    
    const l1 = getLuminance(foreground);
    const l2 = getLuminance(background);
    const ratio = (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
    
    return ratio >= 4.5; // WCAG AA standard for normal text
  }
};

// Screen reader utilities
export const screenReader = {
  /**
   * Checks if screen reader is active
   */
  isActive: (): boolean => {
    // Check common screen reader indicators
    return !!(
      window.navigator.userAgent.match(/NVDA|JAWS|VoiceOver|TalkBack/) ||
      window.speechSynthesis ||
      document.body.classList.contains('screen-reader-active')
    );
  },

  /**
   * Optimizes content for screen readers
   */
  optimizeContent: (element: HTMLElement): void => {
    // Add skip links
    const skipLink = document.createElement('a');
    skipLink.href = '#main-content';
    skipLink.textContent = 'Pular para o conteúdo principal';
    skipLink.className = 'skip-link sr-only-focusable';
    element.prepend(skipLink);
    
    // Ensure proper heading structure
    const headings = element.querySelectorAll('h1, h2, h3, h4, h5, h6');
    let expectedLevel = 1;
    
    headings.forEach((heading) => {
      const currentLevel = parseInt(heading.tagName.charAt(1));
      if (currentLevel > expectedLevel + 1) {
        console.warn('Heading level skipped:', heading);
      }
      expectedLevel = Math.max(expectedLevel, currentLevel);
    });
  }
};

// Touch and mobile accessibility
export const touch = {
  /**
   * Ensures minimum touch target size (44px x 44px)
   */
  validateTouchTargets: (container: HTMLElement): void => {
    const touchTargets = container.querySelectorAll('button, a, input, [role="button"]');
    
    touchTargets.forEach((target) => {
      const rect = target.getBoundingClientRect();
      if (rect.width < 44 || rect.height < 44) {
        console.warn('Touch target too small:', target);
        (target as HTMLElement).style.minHeight = '44px';
        (target as HTMLElement).style.minWidth = '44px';
      }
    });
  },

  /**
   * Handles touch gestures for accessibility
   */
  setupTouchGestures: (element: HTMLElement): void => {
    let touchStartY = 0;
    
    element.addEventListener('touchstart', (e) => {
      touchStartY = e.touches[0].clientY;
    });
    
    element.addEventListener('touchend', (e) => {
      const touchEndY = e.changedTouches[0].clientY;
      const diff = touchStartY - touchEndY;
      
      // Simple swipe detection for navigation
      if (Math.abs(diff) > 50) {
        if (diff > 0) {
          // Swipe up
          manageFocus.announce('Navegando para cima');
        } else {
          // Swipe down
          manageFocus.announce('Navegando para baixo');
        }
      }
    });
  }
};

// Accessibility testing utilities
export const testing = {
  /**
   * Runs basic accessibility checks
   */
  runBasicChecks: (container: HTMLElement = document.body): Promise<string[]> => {
    return new Promise((resolve) => {
      const issues: string[] = [];
      
      // Check for missing alt text
      const images = container.querySelectorAll('img');
      images.forEach((img) => {
        if (!img.alt && img.getAttribute('role') !== 'presentation') {
          issues.push(`Image missing alt text: ${img.src}`);
        }
      });
      
      // Check for missing form labels
      const inputs = container.querySelectorAll('input, select, textarea');
      inputs.forEach((input) => {
        const formElement = input as HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;
        const label = container.querySelector(`label[for="${formElement.id}"]`);
        const ariaLabel = formElement.getAttribute('aria-label');
        const ariaLabelledBy = formElement.getAttribute('aria-labelledby');
        
        if (!label && !ariaLabel && !ariaLabelledBy) {
          issues.push(`Form control missing label: ${formElement.id || (formElement as HTMLInputElement).name}`);
        }
      });
      
      // Check for proper heading structure
      const headings = container.querySelectorAll('h1, h2, h3, h4, h5, h6');
      let lastLevel = 0;
      headings.forEach((heading) => {
        const level = parseInt(heading.tagName.charAt(1));
        if (level > lastLevel + 1) {
          issues.push(`Heading level skipped: ${heading.textContent}`);
        }
        lastLevel = level;
      });
      
      resolve(issues);
    });
  }
};

export default {
  manageFocus,
  aria,
  keyboard,
  contrast,
  screenReader,
  touch,
  testing
};
