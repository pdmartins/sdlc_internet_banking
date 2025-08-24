/**
 * Main Setup for User Story 5.1.1: Accessibility and Performance
 * WCAG 2.1 AA Compliance and <200ms Core Flow Response Time
 */

import accessibility from './accessibility';
import performanceUtils, { performanceMonitor } from './performance';
import responsive from './responsive';
import accessibilityTesting from './accessibilityTesting';

// Initialize accessibility and performance monitoring
export const initializeAccessibilityAndPerformance = (): void => {
  console.log('🚀 Initializing Accessibility and Performance Features...');

  // 1. Setup Web Vitals monitoring
  performanceMonitor.measureWebVitals();

  // 2. Setup core flow monitoring
  performanceUtils.coreFlows.monitorLoginFlow();
  performanceUtils.coreFlows.monitorTransactionFlow();
  performanceUtils.coreFlows.monitorNavigation();

  // 3. Setup responsive design
  responsive.viewport.setupMobileViewport();
  
  // 4. Setup orientation handling
  responsive.orientation.setupOrientationHandling((orientation) => {
    console.log(`Orientation changed to: ${orientation}`);
    accessibility.manageFocus.announce(`Orientação alterada para ${orientation === 'landscape' ? 'paisagem' : 'retrato'}`);
  });

  // 5. Setup accessibility features
  setupAccessibilityFeatures();

  // 6. Setup lazy loading for images
  responsive.responsiveImage.setupLazyLoading();

  // 7. Setup touch interactions for mobile
  if (responsive.device.isTouch()) {
    setupTouchAccessibility();
  }

  // 8. Run initial accessibility audit in development
  if (process.env.NODE_ENV === 'development') {
    setTimeout(() => {
      runAccessibilityAudit();
    }, 2000); // Wait for app to load
  }

  console.log('✅ Accessibility and Performance initialization complete');
};

// Setup accessibility features
const setupAccessibilityFeatures = (): void => {
  // 1. Optimize content for screen readers
  accessibility.screenReader.optimizeContent(document.body);

  // 2. Setup global keyboard navigation
  document.addEventListener('keydown', (e) => {
    // Global keyboard shortcuts
    if (e.altKey && e.key === '1') {
      // Skip to main content
      accessibility.manageFocus.setFocus('#main-content');
      e.preventDefault();
    }
    
    if (e.altKey && e.key === '2') {
      // Skip to navigation
      accessibility.manageFocus.setFocus('nav');
      e.preventDefault();
    }
  });

  // 3. Setup focus management for SPAs
  let lastFocusedElement: HTMLElement | null = null;
  
  window.addEventListener('beforeunload', () => {
    lastFocusedElement = document.activeElement as HTMLElement;
  });

  window.addEventListener('load', () => {
    if (lastFocusedElement) {
      lastFocusedElement.focus();
    } else {
      // Focus first heading or main content
      const firstHeading = document.querySelector('h1, h2, #main-content') as HTMLElement;
      if (firstHeading) {
        firstHeading.focus();
      }
    }
  });

  // 4. Announce page changes for SPAs
  const announcePageChange = (title: string) => {
    accessibility.manageFocus.announce(`Página carregada: ${title}`, 'assertive');
  };

  // Listen for route changes (if using React Router)
  window.addEventListener('popstate', () => {
    const title = document.title;
    announcePageChange(title);
  });

  // 5. Setup high contrast mode detection
  if (window.matchMedia && window.matchMedia('(prefers-contrast: high)').matches) {
    document.body.classList.add('high-contrast');
    console.log('High contrast mode detected');
  }

  // 6. Setup reduced motion detection
  if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
    document.body.classList.add('reduced-motion');
    console.log('Reduced motion preference detected');
  }
};

// Setup touch accessibility
const setupTouchAccessibility = (): void => {
  // Ensure minimum touch target sizes
  accessibility.touch.validateTouchTargets(document.body);

  // Setup touch gestures for navigation
  accessibility.touch.setupTouchGestures(document.body);

  // Improve scrolling for touch devices
  document.body.style.overflow = 'auto';
  (document.body.style as any).webkitOverflowScrolling = 'touch';
};

// Run accessibility audit
const runAccessibilityAudit = async (): Promise<void> => {
  try {
    console.log('🔍 Running accessibility audit...');
    
    const results = await accessibilityTesting.accessibilityTesting.runAudit();
    accessibilityTesting.accessibilityTesting.reportIssues(results);
    
    // Run specific feature tests
    const keyboardIssues = await accessibilityTesting.accessibilityTesting.testFeatures.keyboardNavigation(document.body);
    if (keyboardIssues.length > 0) {
      console.warn('Keyboard navigation issues:', keyboardIssues);
    }
    
    const formIssues = accessibilityTesting.accessibilityTesting.testFeatures.formAccessibility(document.body);
    if (formIssues.length > 0) {
      console.warn('Form accessibility issues:', formIssues);
    }
    
    const headingIssues = accessibilityTesting.accessibilityTesting.testFeatures.headingStructure(document.body);
    if (headingIssues.length > 0) {
      console.warn('Heading structure issues:', headingIssues);
    }
    
    const imageIssues = accessibilityTesting.accessibilityTesting.testFeatures.imageAccessibility(document.body);
    if (imageIssues.length > 0) {
      console.warn('Image accessibility issues:', imageIssues);
    }
    
    const ariaIssues = accessibilityTesting.accessibilityTesting.testFeatures.ariaUsage(document.body);
    if (ariaIssues.length > 0) {
      console.warn('ARIA usage issues:', ariaIssues);
    }
    
  } catch (error) {
    console.error('Accessibility audit failed:', error);
  }
};

// Performance monitoring helpers
export const performanceHelpers = {
  /**
   * Measures component render performance
   */
  measureComponent: (name: string, renderFn: () => void): void => {
    performanceMonitor.measureRender(name, renderFn);
  },

  /**
   * Measures API call performance
   */
  measureApi: async <T>(name: string, apiCall: () => Promise<T>): Promise<T> => {
    return performanceMonitor.measureApiCall(name, apiCall);
  },

  /**
   * Gets performance summary
   */
  getSummary: () => {
    return performanceMonitor.getMetricsSummary();
  }
};

// Accessibility helpers
export const accessibilityHelpers = {
  /**
   * Announces message to screen readers
   */
  announce: (message: string, priority: 'polite' | 'assertive' = 'polite'): void => {
    accessibility.manageFocus.announce(message, priority);
  },

  /**
   * Sets focus to element
   */
  setFocus: (selector: string): void => {
    accessibility.manageFocus.setFocus(selector);
  },

  /**
   * Sets up ARIA relationships for form fields
   */
  setupFieldAria: (fieldId: string, errorId?: string, helpId?: string) => {
    return accessibility.aria.setupFieldAria(fieldId, errorId, helpId);
  },

  /**
   * Handles keyboard activation
   */
  handleActivation: (callback: () => void) => {
    return accessibility.keyboard.handleActivation(callback);
  }
};

// Responsive helpers
export const responsiveHelpers = {
  /**
   * Gets current device type
   */
  getDeviceType: () => {
    return responsive.device.getType();
  },

  /**
   * Checks if element is in viewport
   */
  isInViewport: (element: HTMLElement): boolean => {
    return responsive.viewport.isInViewport(element);
  },

  /**
   * Scrolls element into view
   */
  scrollIntoView: (element: HTMLElement): void => {
    responsive.viewport.scrollIntoView(element);
  }
};

// Export main initialization function
export default {
  initializeAccessibilityAndPerformance,
  performanceHelpers,
  accessibilityHelpers,
  responsiveHelpers
};
