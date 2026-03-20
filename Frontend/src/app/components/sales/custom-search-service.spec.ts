import { TestBed } from '@angular/core/testing';

import { CustomSearchService } from './custom-search-service';

describe('CustomSearchService', () => {
  let service: CustomSearchService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CustomSearchService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
