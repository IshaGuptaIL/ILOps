import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BatchOutputNoticeComponent } from './batch-output-notice-component';

describe('BatchOutputNoticeComponent', () => {
  let component: BatchOutputNoticeComponent;
  let fixture: ComponentFixture<BatchOutputNoticeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BatchOutputNoticeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BatchOutputNoticeComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
