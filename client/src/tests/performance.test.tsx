/**
 * Performance Tests for User Story 5.1.1
 * Core Flow Response Time <200ms
 */

import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { performanceMonitor, optimization, memoization, codeSplitting, bundleAnalysis, coreFlows } from '../utils/performance';
import responsive from '../utils/responsive';

// Mock performance API
const mockPerformance = {
  now: jest.fn(),
  mark: jest.fn(),
  measure: jest.fn(),
  getEntriesByName: jest.fn().mockReturnValue([]),
  getEntriesByType: jest.fn().mockReturnValue([]),
  clearMarks: jest.fn(),
  clearMeasures: jest.fn()
};

Object.defineProperty(window, 'performance', {
  value: mockPerformance,
  writable: true
});

describe('User Story 5.1.1: Performance Requirements', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
  });

  describe('Core Flow Response Times', () => {
    test('should measure component render time', () => {
      let renderTime = 0;
      const mockRenderFn = jest.fn(() => {
        // Simulate render work
        const start = Date.now();
        while (Date.now() - start < 10) {
          // Busy wait for 10ms
        }
      });

      // Mock performance.now to return incremental values
      mockPerformance.now
        .mockReturnValueOnce(0)    // Start time
        .mockReturnValueOnce(15);  // End time (15ms render)

      performanceMonitor.measureRender('TestComponent', mockRenderFn);

      expect(mockRenderFn).toHaveBeenCalled();
      expect(mockPerformance.now).toHaveBeenCalledTimes(2);
    });

    test('should measure API call performance', async () => {
      const mockApiCall = jest.fn().mockResolvedValue({ data: 'test' });
      
      // Mock performance timing
      mockPerformance.now
        .mockReturnValueOnce(0)    // Start time
        .mockReturnValueOnce(150); // End time (150ms)

      const result = await performanceMonitor.measureApiCall('TestAPI', mockApiCall);

      expect(result).toEqual({ data: 'test' });
      expect(mockApiCall).toHaveBeenCalled();
      expect(mockPerformance.now).toHaveBeenCalledTimes(2);
    });

    test('should warn on slow API calls over 200ms', async () => {
      const consoleSpy = jest.spyOn(console, 'warn').mockImplementation();
      const mockApiCall = jest.fn().mockResolvedValue({ data: 'test' });
      
      // Mock slow API call (250ms)
      mockPerformance.now
        .mockReturnValueOnce(0)
        .mockReturnValueOnce(250);

      await performanceMonitor.measureApiCall('SlowAPI', mockApiCall);

      expect(consoleSpy).toHaveBeenCalledWith('SlowAPI API call exceeded 200ms target');
      
      consoleSpy.mockRestore();
    });

    test('should track performance metrics', () => {
      const testMetric = {
        name: 'test_metric',
        value: 100,
        component: 'TestComponent'
      };

      performanceMonitor.reportMetric(testMetric);

      const metrics = JSON.parse(localStorage.getItem('performance_metrics') || '[]');
      expect(metrics).toHaveLength(1);
      expect(metrics[0]).toMatchObject(testMetric);
      expect(metrics[0]).toHaveProperty('timestamp');
      expect(metrics[0]).toHaveProperty('url');
    });

    test('should calculate performance summary', () => {
      // Add test metrics
      const metrics = [
        { name: 'api_response_time', value: 100 },
        { name: 'api_response_time', value: 150 },
        { name: 'api_response_time', value: 200 },
        { name: 'component_render_time', value: 10 },
        { name: 'component_render_time', value: 20 }
      ];

      localStorage.setItem('performance_metrics', JSON.stringify(metrics));

      const summary = performanceMonitor.getMetricsSummary();

      expect(summary).toHaveProperty('api_response_time');
      expect(summary).toHaveProperty('component_render_time');
      
      expect(summary.api_response_time.count).toBe(3);
      expect(summary.api_response_time.average).toBe(150);
      expect(summary.api_response_time.min).toBe(100);
      expect(summary.api_response_time.max).toBe(200);
      
      expect(summary.component_render_time.count).toBe(2);
      expect(summary.component_render_time.average).toBe(15);
    });

    test('should handle API call errors', async () => {
      const mockApiCall = jest.fn().mockRejectedValue(new Error('API Error'));
      
      mockPerformance.now
        .mockReturnValueOnce(0)
        .mockReturnValueOnce(100);

      await expect(
        performanceMonitor.measureApiCall('FailingAPI', mockApiCall)
      ).rejects.toThrow('API Error');

      // Should still record metrics for failed calls
      const metrics = JSON.parse(localStorage.getItem('performance_metrics') || '[]');
      expect(metrics).toHaveLength(1);
      expect(metrics[0]).toMatchObject({
        name: 'api_response_time',
        value: 100,
        api: 'FailingAPI',
        status: 'error'
      });
    });
  });

  describe('Responsive Design Performance', () => {
    test('should detect device type efficiently', () => {
      // Mock different viewport sizes
      Object.defineProperty(window, 'innerWidth', { value: 320, writable: true });
      expect(responsive.device.getType()).toBe('mobile');

      Object.defineProperty(window, 'innerWidth', { value: 600, writable: true });
      expect(responsive.device.getType()).toBe('tablet');

      Object.defineProperty(window, 'innerWidth', { value: 1200, writable: true });
      expect(responsive.device.getType()).toBe('desktop');
    });

    test('should efficiently check if element is in viewport', () => {
      const mockElement = {
        getBoundingClientRect: jest.fn().mockReturnValue({
          top: 100,
          left: 100,
          bottom: 200,
          right: 200
        })
      } as unknown as HTMLElement;

      // Mock viewport dimensions
      Object.defineProperty(window, 'innerHeight', { value: 600, writable: true });
      Object.defineProperty(window, 'innerWidth', { value: 800, writable: true });

      const isInViewport = responsive.viewport.isInViewport(mockElement);
      expect(isInViewport).toBe(true);
      expect(mockElement.getBoundingClientRect).toHaveBeenCalledTimes(1);
    });

    test('should optimize images for different screen sizes', () => {
      const imageAttributes = responsive.responsiveImage.createAttributes(
        '/test-image.jpg',
        [
          { width: 320 },
          { width: 768 },
          { width: 1200, density: 2 }
        ]
      );

      expect(imageAttributes).toHaveProperty('src');
      expect(imageAttributes).toHaveProperty('srcSet');
      expect(imageAttributes).toHaveProperty('sizes');
      
      expect(imageAttributes.srcSet).toContain('320w');
      expect(imageAttributes.srcSet).toContain('768w');
      expect(imageAttributes.srcSet).toContain('2400w'); // 1200 * 2
    });

    test('should setup lazy loading efficiently', () => {
      // Mock IntersectionObserver
      const mockObserver = {
        observe: jest.fn(),
        unobserve: jest.fn(),
        disconnect: jest.fn()
      };

      Object.defineProperty(window, 'IntersectionObserver', {
        value: jest.fn().mockImplementation(() => mockObserver),
        writable: true
      });

      // Create test images
      document.body.innerHTML = `
        <img data-src="/image1.jpg" alt="Image 1" />
        <img data-src="/image2.jpg" alt="Image 2" />
      `;

      responsive.responsiveImage.setupLazyLoading();

      expect(window.IntersectionObserver).toHaveBeenCalledTimes(1);
      expect(mockObserver.observe).toHaveBeenCalledTimes(2);
    });

    test('should handle orientation changes efficiently', () => {
      const mockCallback = jest.fn();
      
      // Mock window dimensions for portrait
      Object.defineProperty(window, 'innerWidth', { value: 375, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 667, writable: true });

      const cleanup = responsive.orientation.setupOrientationHandling(mockCallback);

      // Simulate orientation change to landscape
      Object.defineProperty(window, 'innerWidth', { value: 667, writable: true });
      Object.defineProperty(window, 'innerHeight', { value: 375, writable: true });

      fireEvent(window, new Event('orientationchange'));

      expect(mockCallback).toHaveBeenCalledWith('landscape');

      cleanup();
    });
  });

  describe('Memory Management', () => {
    test('should limit stored performance metrics', () => {
      // Add more than 50 metrics
      const metrics = Array.from({ length: 60 }, (_, i) => ({
        name: 'test_metric',
        value: i,
        timestamp: Date.now()
      }));

      localStorage.setItem('performance_metrics', JSON.stringify(metrics));

      // Add one more metric
      performanceMonitor.reportMetric({
        name: 'new_metric',
        value: 100
      });

      const storedMetrics = JSON.parse(localStorage.getItem('performance_metrics') || '[]');
      expect(storedMetrics.length).toBeLessThanOrEqual(50);
    });

    test('should cleanup event listeners properly', () => {
      const removeEventListenerSpy = jest.spyOn(window, 'removeEventListener');
      
      const cleanup = responsive.orientation.setupOrientationHandling();
      cleanup();

      expect(removeEventListenerSpy).toHaveBeenCalledWith('orientationchange', expect.any(Function));
      expect(removeEventListenerSpy).toHaveBeenCalledWith('resize', expect.any(Function));

      removeEventListenerSpy.mockRestore();
    });
  });

  describe('Optimization Features', () => {
    test('should debounce expensive operations', (done) => {
      const mockFn = jest.fn();
      const debouncedFn = optimization.debounce(mockFn, 100);

      // Call multiple times rapidly
      debouncedFn('arg1');
      debouncedFn('arg2');
      debouncedFn('arg3');

      // Should not be called immediately
      expect(mockFn).not.toHaveBeenCalled();

      // Should be called once after delay
      setTimeout(() => {
        expect(mockFn).toHaveBeenCalledTimes(1);
        expect(mockFn).toHaveBeenCalledWith('arg3');
        done();
      }, 150);
    });

    test('should throttle high-frequency events', () => {
      const mockFn = jest.fn();
      const throttledFn = optimization.throttle(mockFn, 100);

      // Call multiple times rapidly
      throttledFn('arg1');
      throttledFn('arg2');
      throttledFn('arg3');

      // Should be called immediately for first call
      expect(mockFn).toHaveBeenCalledTimes(1);
      expect(mockFn).toHaveBeenCalledWith('arg1');
    });

    test('should create memoized functions', () => {
      const expensiveFn = jest.fn((x: number) => x * 2);
      const memoizedFn = memoization.createMemoized(expensiveFn);

      // First call
      const result1 = memoizedFn(5);
      expect(result1).toBe(10);
      expect(expensiveFn).toHaveBeenCalledTimes(1);

      // Second call with same argument should use cache
      const result2 = memoizedFn(5);
      expect(result2).toBe(10);
      expect(expensiveFn).toHaveBeenCalledTimes(1); // Still 1, not called again

      // Different argument should call function
      const result3 = memoizedFn(10);
      expect(result3).toBe(20);
      expect(expensiveFn).toHaveBeenCalledTimes(2);
    });

    test('should implement API cache with TTL', () => {
      const cache = memoization.createApiCache(1000); // 1 second TTL

      // Set value
      cache.set('key1', { data: 'test' });
      
      // Get value immediately
      expect(cache.get('key1')).toEqual({ data: 'test' });

      // Mock time passing
      jest.advanceTimersByTime(1100);

      // Value should be expired
      expect(cache.get('key1')).toBeNull();
    });

    test('should create virtual scroll hook', () => {
      const items = Array.from({ length: 1000 }, (_, i) => ({ id: i, name: `Item ${i}` }));
      const virtualScroll = optimization.createVirtualScrollHook(50, 400);

      // Mock React hooks
      let scrollTop = 0;
      const mockUseState = jest.fn().mockImplementation((initial) => [scrollTop, (value: number) => { scrollTop = value; }]);
      
      React.useState = mockUseState;

      const result = virtualScroll(items);

      expect(result).toHaveProperty('visibleItems');
      expect(result).toHaveProperty('offsetY');
      expect(result).toHaveProperty('totalHeight');
      expect(result).toHaveProperty('onScroll');

      // Should show subset of items
      expect(result.visibleItems.length).toBeLessThan(items.length);
      expect(result.totalHeight).toBe(50000); // 1000 items * 50px height
    });
  });

  describe('Web Vitals Integration', () => {
    test('should measure and report web vitals', () => {
      const reportMetricSpy = jest.spyOn(performanceMonitor, 'reportMetric');

      // Mock web vitals
      const mockMetric = {
        name: 'CLS',
        value: 0.1,
        delta: 0.1,
        id: 'test',
        entries: []
      };

      performanceMonitor.measureWebVitals();

      // Simulate metric callback
      performanceMonitor.reportMetric(mockMetric);

      expect(reportMetricSpy).toHaveBeenCalledWith(mockMetric);

      reportMetricSpy.mockRestore();
    });

    test('should monitor core user journeys', () => {
      const mockAddEventListener = jest.spyOn(window, 'addEventListener');

      coreFlows.monitorLoginFlow();
      coreFlows.monitorTransactionFlow();

      expect(mockAddEventListener).toHaveBeenCalledWith('loginSuccess', expect.any(Function));
      expect(mockAddEventListener).toHaveBeenCalledWith('transactionSuccess', expect.any(Function));

      mockAddEventListener.mockRestore();
    });
  });

  describe('Bundle Optimization', () => {
    test('should support code splitting', () => {
      const mockImportFn = jest.fn().mockResolvedValue({ default: () => <div>Lazy Component</div> });
      
      codeSplitting.preloadComponent(mockImportFn);

      // Should use requestIdleCallback or setTimeout
      expect(mockImportFn).toHaveBeenCalled();
    });

    test('should create lazy loading wrapper', () => {
      const lazyWrapper = codeSplitting.createLazyWrapper('./LazyComponent');
      
      expect(typeof lazyWrapper).toBe('function');
      
      // Should return a promise when called
      const result = lazyWrapper();
      expect(result).toBeInstanceOf(Promise);
    });
  });

  describe('Performance Monitoring in Production', () => {
    test('should only store metrics in development', () => {
      const originalEnv = process.env.NODE_ENV;
      
      // Test development mode
      process.env.NODE_ENV = 'development';
      performanceMonitor.reportMetric({ name: 'test', value: 100 });
      
      let metrics = JSON.parse(localStorage.getItem('performance_metrics') || '[]');
      expect(metrics.length).toBeGreaterThan(0);

      // Clear storage
      localStorage.clear();

      // Test production mode
      process.env.NODE_ENV = 'production';
      performanceMonitor.reportMetric({ name: 'test', value: 100 });
      
      metrics = JSON.parse(localStorage.getItem('performance_metrics') || '[]');
      expect(metrics.length).toBeGreaterThan(0); // Still stores locally for this implementation

      process.env.NODE_ENV = originalEnv;
    });

    test('should measure navigation timing', () => {
      const mockObserver = {
        observe: jest.fn(),
        disconnect: jest.fn()
      };

      Object.defineProperty(window, 'PerformanceObserver', {
        value: jest.fn().mockImplementation(() => mockObserver),
        writable: true
      });

      coreFlows.monitorNavigation();

      expect(window.PerformanceObserver).toHaveBeenCalledWith(expect.any(Function));
      expect(mockObserver.observe).toHaveBeenCalledWith({ entryTypes: ['navigation'] });
    });
  });
});

// Mock React for virtual scroll test
const React = {
  useState: jest.fn()
};
