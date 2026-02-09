import { TestBed } from '@angular/core/testing';

import { ImeiService } from './imei-service';

describe('ImeiService', () => {
  let service: ImeiService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ImeiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
