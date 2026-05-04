import { TestBed } from '@angular/core/testing';

import { AdvantageVoiceService } from './advantage-voice-service';

describe('AdvantageVoiceService', () => {
  let service: AdvantageVoiceService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AdvantageVoiceService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
