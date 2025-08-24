/**
 * Performance Utilities for Core Flow Response Time <200ms
 * User Story 5.1.1: Accessibility and Performance
 */

import React from 'react';
import { getCLS, getFID, getFCP, getLCP, getTTFB } from 'web-vitals';

// Performance monitoring
export const performanceMonitor = {
  /**
   * Measures and reports Web Vitals
   */
  measureWebVitals: (): void => {
    getCLS((metric) => {
      console.log('CLS:', metric);
      performanceMonitor.reportMetric(metric);
    });

    getFID((metric) => {
      console.log('FID:', metric);
      performanceMonitor.reportMetric(metric);
    });

    getFCP((metric) => {
      console.log('FCP:', metric);
      performanceMonitor.reportMetric(metric);
    });

    getLCP((metric) => {
      console.log('LCP:', metric);
      performanceMonitor.reportMetric(metric);
    });

    getTTFB((metric) => {
      console.log('TTFB:', metric);
      performanceMonitor.reportMetric(metric);
    });
  },

  /**
   * Reports performance metrics to monitoring service
   */
  reportMetric: (metric: any): void => {
    // In production, send to analytics service
    if (process.env.NODE_ENV === 'production') {
      // Example: send to Azure Application Insights
      // window.appInsights?.trackMetric({ name: metric.name, average: metric.value });
    }
    
    // Store locally for development
    const metrics = JSON.parse(localStorage.getItem('performance_metrics') || '[]');
    metrics.push({
      ...metric,
      timestamp: Date.now(),
      url: window.location.pathname
    });
    
    // Keep only last 50 metrics
    if (metrics.length > 50) {
      metrics.splice(0, metrics.length - 50);
    }
    
    localStorage.setItem('performance_metrics', JSON.stringify(metrics));
  },

  /**
   * Measures component render time
   */
  measureRender: (componentName: string, renderFn: () => void): void => {
    const start = performance.now();
    renderFn();
    const end = performance.now();
    const duration = end - start;
    
    console.log(`${componentName} render time: ${duration.toFixed(2)}ms`);
    
    if (duration > 16.67) { // More than one frame at 60fps
      console.warn(`${componentName} render time exceeded 16.67ms`);
    }
    
    performanceMonitor.reportMetric({
      name: 'component_render_time',
      value: duration,
      component: componentName
    });
  },

  /**
   * Monitors API response times
   */
  measureApiCall: async <T>(
    apiName: string,
    apiCall: () => Promise<T>
  ): Promise<T> => {
    const start = performance.now();
    
    try {
      const result = await apiCall();
      const end = performance.now();
      const duration = end - start;
      
      console.log(`${apiName} API call: ${duration.toFixed(2)}ms`);
      
      if (duration > 200) {
        console.warn(`${apiName} API call exceeded 200ms target`);
      }
      
      performanceMonitor.reportMetric({
        name: 'api_response_time',
        value: duration,
        api: apiName,
        status: 'success'
      });
      
      return result;
    } catch (error) {
      const end = performance.now();
      const duration = end - start;
      
      performanceMonitor.reportMetric({
        name: 'api_response_time',
        value: duration,
        api: apiName,
        status: 'error'
      });
      
      throw error;
    }
  },

  /**
   * Gets performance metrics summary
   */
  getMetricsSummary: (): any => {
    const metrics = JSON.parse(localStorage.getItem('performance_metrics') || '[]');
    
    const summary = metrics.reduce((acc: any, metric: any) => {
      if (!acc[metric.name]) {
        acc[metric.name] = {
          count: 0,
          total: 0,
          min: Infinity,
          max: -Infinity,
          values: []
        };
      }
      
      acc[metric.name].count++;
      acc[metric.name].total += metric.value;
      acc[metric.name].min = Math.min(acc[metric.name].min, metric.value);
      acc[metric.name].max = Math.max(acc[metric.name].max, metric.value);
      acc[metric.name].values.push(metric.value);
      
      return acc;
    }, {});
    
    // Calculate averages and percentiles
    Object.keys(summary).forEach(key => {
      const data = summary[key];
      data.average = data.total / data.count;
      data.values.sort((a: number, b: number) => a - b);
      data.p75 = data.values[Math.floor(data.values.length * 0.75)];
      data.p95 = data.values[Math.floor(data.values.length * 0.95)];
    });
    
    return summary;
  }
};

// Code splitting utilities
export const codeSplitting = {
  /**
   * Preload components for better UX
   */
  preloadComponent: (importFn: () => Promise<any>): void => {
    // Preload on idle
    if ('requestIdleCallback' in window) {
      requestIdleCallback(() => {
        importFn();
      });
    } else {
      setTimeout(() => {
        importFn();
      }, 100);
    }
  },

  /**
   * Create lazy loading wrapper
   */
  createLazyWrapper: (importPath: string) => {
    return () => import(importPath);
  }
};

// Memoization utilities
export const memoization = {
  /**
   * Creates a memoized function with cache
   */
  createMemoized: <TArgs extends any[], TReturn>(
    fn: (...args: TArgs) => TReturn,
    keyFn?: (...args: TArgs) => string
  ) => {
    const cache = new Map<string, TReturn>();
    
    return (...args: TArgs): TReturn => {
      const key = keyFn ? keyFn(...args) : JSON.stringify(args);
      
      if (cache.has(key)) {
        return cache.get(key)!;
      }
      
      const result = fn(...args);
      cache.set(key, result);
      
      return result;
    };
  },

  /**
   * Memoizes API responses with TTL
   */
  createApiCache: <T>(ttlMs: number = 300000) => { // 5 minutes default
    const cache = new Map<string, { data: T; timestamp: number }>();
    
    return {
      get: (key: string): T | null => {
        const entry = cache.get(key);
        if (entry && Date.now() - entry.timestamp < ttlMs) {
          return entry.data;
        }
        cache.delete(key);
        return null;
      },
      
      set: (key: string, data: T): void => {
        cache.set(key, { data, timestamp: Date.now() });
      },
      
      clear: (): void => {
        cache.clear();
      }
    };
  }
};

// Resource optimization
export const optimization = {
  /**
   * Optimizes images for different screen sizes
   */
  createResponsiveImage: (
    src: string,
    alt: string,
    sizes: { width: number; density?: number }[]
  ): React.ReactElement => {
    const srcSet = sizes.map(size => {
      const density = size.density || 1;
      return `${src}?w=${size.width}&dpr=${density} ${size.width * density}w`;
    }).join(', ');
    
    return React.createElement('img', {
      src: `${src}?w=${sizes[0].width}`,
      srcSet,
      sizes: sizes.map(size => `(max-width: ${size.width}px) ${size.width}px`).join(', '),
      alt,
      loading: 'lazy',
      decoding: 'async'
    });
  },

  /**
   * Debounces expensive operations
   */
  debounce: <T extends (...args: any[]) => any>(
    func: T,
    wait: number
  ): ((...args: Parameters<T>) => void) => {
    let timeout: NodeJS.Timeout;
    
    return (...args: Parameters<T>) => {
      clearTimeout(timeout);
      timeout = setTimeout(() => func.apply(null, args), wait);
    };
  },

  /**
   * Throttles high-frequency events
   */
  throttle: <T extends (...args: any[]) => any>(
    func: T,
    limit: number
  ): ((...args: Parameters<T>) => void) => {
    let inThrottle: boolean;
    
    return (...args: Parameters<T>) => {
      if (!inThrottle) {
        func.apply(null, args);
        inThrottle = true;
        setTimeout(() => inThrottle = false, limit);
      }
    };
  },

  /**
   * Implements virtual scrolling for large lists
   */
  createVirtualScrollHook: (itemHeight: number, containerHeight: number) => {
    return (items: any[]) => {
      const [scrollTop, setScrollTop] = React.useState(0);
      
      const visibleCount = Math.ceil(containerHeight / itemHeight);
      const startIndex = Math.floor(scrollTop / itemHeight);
      const endIndex = Math.min(startIndex + visibleCount + 1, items.length);
      
      const visibleItems = items.slice(startIndex, endIndex);
      const offsetY = startIndex * itemHeight;
      
      return {
        visibleItems,
        offsetY,
        totalHeight: items.length * itemHeight,
        onScroll: (e: React.UIEvent<HTMLDivElement>) => {
          setScrollTop(e.currentTarget.scrollTop);
        }
      };
    };
  }
};

// Bundle analysis
export const bundleAnalysis = {
  /**
   * Analyzes bundle size and dependencies
   */
  analyzeBundleSize: (): void => {
    if (process.env.NODE_ENV === 'development') {
      console.log('Bundle analysis available in production build');
      console.log('Run: npm run analyze');
    }
  },

  /**
   * Checks for unused dependencies
   */
  checkUnusedDependencies: (): void => {
    // This would be implemented with webpack-bundle-analyzer
    console.log('Checking for unused dependencies...');
    console.log('Use webpack-bundle-analyzer for detailed analysis');
  }
};

// Core flow performance monitoring
export const coreFlows = {
  /**
   * Monitors login flow performance
   */
  monitorLoginFlow: (): void => {
    const startTime = performance.now();
    
    window.addEventListener('loginSuccess', () => {
      const duration = performance.now() - startTime;
      console.log(`Login flow completed in: ${duration.toFixed(2)}ms`);
      
      if (duration > 2000) {
        console.warn('Login flow exceeded 2 second target');
      }
      
      performanceMonitor.reportMetric({
        name: 'login_flow_duration',
        value: duration
      });
    });
  },

  /**
   * Monitors transaction flow performance
   */
  monitorTransactionFlow: (): void => {
    const startTime = performance.now();
    
    window.addEventListener('transactionSuccess', () => {
      const duration = performance.now() - startTime;
      console.log(`Transaction flow completed in: ${duration.toFixed(2)}ms`);
      
      if (duration > 3000) {
        console.warn('Transaction flow exceeded 3 second target');
      }
      
      performanceMonitor.reportMetric({
        name: 'transaction_flow_duration',
        value: duration
      });
    });
  },

  /**
   * Monitors navigation performance
   */
  monitorNavigation: (): void => {
    const observer = new PerformanceObserver((list) => {
      list.getEntries().forEach((entry) => {
        if (entry.entryType === 'navigation') {
          const navigationEntry = entry as PerformanceNavigationTiming;
          
          const metrics = {
            dns: navigationEntry.domainLookupEnd - navigationEntry.domainLookupStart,
            connect: navigationEntry.connectEnd - navigationEntry.connectStart,
            request: navigationEntry.responseStart - navigationEntry.requestStart,
            response: navigationEntry.responseEnd - navigationEntry.responseStart,
            dom: navigationEntry.domContentLoadedEventEnd - navigationEntry.domContentLoadedEventStart,
            load: navigationEntry.loadEventEnd - navigationEntry.loadEventStart
          };
          
          console.log('Navigation timing:', metrics);
          
          Object.entries(metrics).forEach(([name, value]) => {
            performanceMonitor.reportMetric({
              name: `navigation_${name}`,
              value
            });
          });
        }
      });
    });
    
    observer.observe({ entryTypes: ['navigation'] });
  }
};

export default {
  performanceMonitor,
  codeSplitting,
  memoization,
  optimization,
  bundleAnalysis,
  coreFlows
};
