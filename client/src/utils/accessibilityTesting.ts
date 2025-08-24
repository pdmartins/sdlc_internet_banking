/**
 * Accessibility Testing Utilities with axe-core Integration
 * User Story 5.1.1: Accessibility and Performance
 */

import axe from 'axe-core';

// WCAG 2.1 AA compliance testing
export const accessibilityTesting = {
  /**
   * Runs comprehensive accessibility audit
   */
  runAudit: async (element: HTMLElement = document.body): Promise<axe.AxeResults> => {
    try {
      return new Promise((resolve, reject) => {
        axe.run(element, (error, results) => {
          if (error) {
            reject(error);
          } else {
            resolve(results);
          }
        });
      });
    } catch (error) {
      console.error('Accessibility audit failed:', error);
      throw error;
    }
  },

  /**
   * Reports accessibility issues in console
   */
  reportIssues: (results: axe.AxeResults): void => {
    console.group('🔍 Accessibility Audit Results');
    
    if (results.violations.length === 0) {
      console.log('✅ No accessibility violations found!');
    } else {
      console.warn(`❌ Found ${results.violations.length} accessibility violations:`);
      
      results.violations.forEach((violation, index) => {
        console.group(`${index + 1}. ${violation.id} (${violation.impact})`);
        console.log('Description:', violation.description);
        console.log('Help:', violation.help);
        console.log('Help URL:', violation.helpUrl);
        console.log('Affected elements:', violation.nodes.length);
        
        violation.nodes.forEach((node, nodeIndex) => {
          console.log(`Element ${nodeIndex + 1}:`, node.target);
          console.log('HTML:', node.html);
          if (node.failureSummary) {
            console.log('Failure:', node.failureSummary);
          }
        });
        
        console.groupEnd();
      });
    }
    
    if (results.incomplete.length > 0) {
      console.warn(`⚠️ ${results.incomplete.length} incomplete checks (manual review needed):`);
      results.incomplete.forEach(item => {
        console.log(`- ${item.id}: ${item.description}`);
      });
    }
    
    if (results.passes.length > 0) {
      console.log(`✅ ${results.passes.length} accessibility checks passed`);
    }
    
    console.groupEnd();
  },

  /**
   * Runs automated accessibility tests for Jest
   */
  createJestMatcher: () => {
    return {
      async toHaveNoViolations(received: HTMLElement) {
        const results = await accessibilityTesting.runAudit(received);
        
        const pass = results.violations.length === 0;
        
        if (pass) {
          return {
            pass: true,
            message: () => 'Expected element to have accessibility violations, but none were found'
          };
        } else {
          const violations = results.violations.map(v => `${v.id}: ${v.description}`).join('\n');
          return {
            pass: false,
            message: () => `Expected element to have no accessibility violations, but found:\n${violations}`
          };
        }
      }
    };
  },

  /**
   * Tests specific accessibility features
   */
  testFeatures: {
    /**
     * Tests keyboard navigation
     */
    keyboardNavigation: async (container: HTMLElement): Promise<string[]> => {
      const issues: string[] = [];
      
      // Check for focusable elements
      const focusableElements = container.querySelectorAll(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      );
      
      if (focusableElements.length === 0) {
        issues.push('No focusable elements found');
        return issues;
      }
      
      // Test tab order
      let currentTabIndex = -1;
      focusableElements.forEach((element, index) => {
        const tabIndex = parseInt((element as HTMLElement).getAttribute('tabindex') || '0');
        
        if (tabIndex > 0 && tabIndex <= currentTabIndex) {
          issues.push(`Invalid tab order at element ${index}: ${element.tagName}`);
        }
        
        if (tabIndex >= 0) {
          currentTabIndex = Math.max(currentTabIndex, tabIndex);
        }
      });
      
      // Check for skip links
      const skipLinks = container.querySelectorAll('a[href^="#"]');
      if (skipLinks.length === 0) {
        issues.push('No skip links found for keyboard navigation');
      }
      
      return issues;
    },

    /**
     * Tests color contrast
     */
    colorContrast: async (container: HTMLElement): Promise<string[]> => {
      const issues: string[] = [];
      
      // This would require a more sophisticated color contrast checker
      // For now, we'll rely on axe-core for comprehensive contrast testing
      return new Promise((resolve, reject) => {
        axe.run(container, (error, results) => {
          if (error) {
            reject(error);
          } else {
            results.violations.forEach(violation => {
              if (violation.id === 'color-contrast') {
                violation.nodes.forEach(node => {
                  issues.push(`Color contrast violation: ${node.html}`);
                });
              }
            });
            resolve(issues);
          }
        });
      });
    },

    /**
     * Tests form accessibility
     */
    formAccessibility: (container: HTMLElement): string[] => {
      const issues: string[] = [];
      
      // Check form inputs have labels
      const inputs = container.querySelectorAll('input, select, textarea');
      inputs.forEach(input => {
        const inputElement = input as HTMLInputElement;
        const id = inputElement.id;
        const label = container.querySelector(`label[for="${id}"]`);
        const ariaLabel = inputElement.getAttribute('aria-label');
        const ariaLabelledBy = inputElement.getAttribute('aria-labelledby');
        
        if (!label && !ariaLabel && !ariaLabelledBy) {
          issues.push(`Form input missing label: ${inputElement.name || inputElement.type}`);
        }
      });
      
      // Check fieldsets have legends
      const fieldsets = container.querySelectorAll('fieldset');
      fieldsets.forEach(fieldset => {
        const legend = fieldset.querySelector('legend');
        if (!legend) {
          issues.push('Fieldset missing legend');
        }
      });
      
      // Check required fields are marked
      const requiredInputs = container.querySelectorAll('input[required], select[required], textarea[required]');
      requiredInputs.forEach(input => {
        const ariaRequired = input.getAttribute('aria-required');
        const requiredAttr = input.hasAttribute('required');
        
        if (!ariaRequired && !requiredAttr) {
          issues.push(`Required field not properly marked: ${(input as HTMLInputElement).name}`);
        }
      });
      
      return issues;
    },

    /**
     * Tests heading structure
     */
    headingStructure: (container: HTMLElement): string[] => {
      const issues: string[] = [];
      const headings = container.querySelectorAll('h1, h2, h3, h4, h5, h6');
      
      if (headings.length === 0) {
        issues.push('No headings found');
        return issues;
      }
      
      let lastLevel = 0;
      headings.forEach((heading, index) => {
        const currentLevel = parseInt(heading.tagName.charAt(1));
        
        if (index === 0 && currentLevel !== 1) {
          issues.push('Page should start with h1');
        }
        
        if (currentLevel > lastLevel + 1) {
          issues.push(`Heading level skipped: ${heading.textContent} (h${currentLevel} after h${lastLevel})`);
        }
        
        lastLevel = currentLevel;
      });
      
      return issues;
    },

    /**
     * Tests image accessibility
     */
    imageAccessibility: (container: HTMLElement): string[] => {
      const issues: string[] = [];
      const images = container.querySelectorAll('img');
      
      images.forEach(img => {
        const alt = img.getAttribute('alt');
        const role = img.getAttribute('role');
        
        if (alt === null && role !== 'presentation' && role !== 'none') {
          issues.push(`Image missing alt text: ${img.src}`);
        }
        
        if (alt === '') {
          // Decorative image, should have role="presentation" or role="none"
          if (role !== 'presentation' && role !== 'none') {
            console.warn(`Decorative image should have role="presentation": ${img.src}`);
          }
        }
        
        if (alt && alt.length > 125) {
          issues.push(`Alt text too long (${alt.length} chars): ${img.src}`);
        }
      });
      
      return issues;
    },

    /**
     * Tests ARIA usage
     */
    ariaUsage: (container: HTMLElement): string[] => {
      const issues: string[] = [];
      
      // Check for elements with ARIA attributes
      const elementsWithAria = container.querySelectorAll('[aria-label], [aria-labelledby], [aria-describedby], [role]');
      
      elementsWithAria.forEach(element => {
        const role = element.getAttribute('role');
        const ariaLabel = element.getAttribute('aria-label');
        const ariaLabelledBy = element.getAttribute('aria-labelledby');
        const ariaDescribedBy = element.getAttribute('aria-describedby');
        
        // Check if aria-labelledby references exist
        if (ariaLabelledBy) {
          const ids = ariaLabelledBy.split(' ');
          ids.forEach(id => {
            if (!container.querySelector(`#${id}`)) {
              issues.push(`aria-labelledby references non-existent ID: ${id}`);
            }
          });
        }
        
        // Check if aria-describedby references exist
        if (ariaDescribedBy) {
          const ids = ariaDescribedBy.split(' ');
          ids.forEach(id => {
            if (!container.querySelector(`#${id}`)) {
              issues.push(`aria-describedby references non-existent ID: ${id}`);
            }
          });
        }
        
        // Check for empty aria-label
        if (ariaLabel === '') {
          issues.push(`Empty aria-label on ${element.tagName}`);
        }
        
        // Check for invalid roles
        const validRoles = [
          'alert', 'alertdialog', 'application', 'article', 'banner', 'button',
          'cell', 'checkbox', 'columnheader', 'combobox', 'complementary',
          'contentinfo', 'definition', 'dialog', 'directory', 'document',
          'form', 'grid', 'gridcell', 'group', 'heading', 'img', 'link',
          'list', 'listbox', 'listitem', 'log', 'main', 'marquee', 'math',
          'menu', 'menubar', 'menuitem', 'menuitemcheckbox', 'menuitemradio',
          'navigation', 'none', 'note', 'option', 'presentation', 'progressbar',
          'radio', 'radiogroup', 'region', 'row', 'rowgroup', 'rowheader',
          'scrollbar', 'search', 'separator', 'slider', 'spinbutton', 'status',
          'tab', 'tablist', 'tabpanel', 'textbox', 'timer', 'toolbar',
          'tooltip', 'tree', 'treegrid', 'treeitem'
        ];
        
        if (role && !validRoles.includes(role)) {
          issues.push(`Invalid ARIA role: ${role} on ${element.tagName}`);
        }
      });
      
      return issues;
    }
  }
};

// Screen reader testing simulation
export const screenReaderTesting = {
  /**
   * Simulates screen reader experience
   */
  simulateScreenReader: (container: HTMLElement): string[] => {
    const content: string[] = [];
    
    const walkDOM = (node: Node) => {
      if (node.nodeType === Node.TEXT_NODE) {
        const text = node.textContent?.trim();
        if (text) {
          content.push(text);
        }
      } else if (node.nodeType === Node.ELEMENT_NODE) {
        const element = node as HTMLElement;
        
        // Add ARIA labels
        const ariaLabel = element.getAttribute('aria-label');
        if (ariaLabel) {
          content.push(`[${ariaLabel}]`);
        }
        
        // Add semantic information
        const role = element.getAttribute('role') || element.tagName.toLowerCase();
        if (['button', 'link', 'heading', 'list', 'listitem'].includes(role)) {
          content.push(`[${role}]`);
        }
        
        // Recursively walk children
        for (const child of Array.from(node.childNodes)) {
          walkDOM(child);
        }
      }
    };
    
    walkDOM(container);
    return content;
  },

  /**
   * Tests reading order
   */
  testReadingOrder: (container: HTMLElement): string[] => {
    const issues: string[] = [];
    const content = screenReaderTesting.simulateScreenReader(container);
    
    // Check for logical reading order
    let hasHeading = false;
    let hasContent = false;
    
    content.forEach((item, index) => {
      if (item.includes('[heading]')) {
        hasHeading = true;
      } else if (item.trim().length > 0 && !item.startsWith('[')) {
        hasContent = true;
      }
    });
    
    if (!hasHeading) {
      issues.push('No headings found for navigation');
    }
    
    if (!hasContent) {
      issues.push('No readable content found');
    }
    
    return issues;
  }
};

// Performance impact of accessibility features
export const accessibilityPerformance = {
  /**
   * Measures performance impact of accessibility features
   */
  measureImpact: async (container: HTMLElement): Promise<void> => {
    const startTime = performance.now();
    
    // Run accessibility audit
    await accessibilityTesting.runAudit(container);
    
    const endTime = performance.now();
    const duration = endTime - startTime;
    
    console.log(`Accessibility audit completed in ${duration.toFixed(2)}ms`);
    
    if (duration > 100) {
      console.warn('Accessibility audit took longer than expected');
    }
  }
};

export default {
  accessibilityTesting,
  screenReaderTesting,
  accessibilityPerformance
};
