import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdvantageVoiceComponent } from './advantage-voice-component';

describe('AdvantageVoiceComponent', () => {
  let component: AdvantageVoiceComponent;
  let fixture: ComponentFixture<AdvantageVoiceComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdvantageVoiceComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdvantageVoiceComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
