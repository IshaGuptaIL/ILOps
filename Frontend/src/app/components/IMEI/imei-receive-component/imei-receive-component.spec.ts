import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ImeiReceiveComponent } from './imei-receive-component';

describe('ImeiReceiveComponent', () => {
  let component: ImeiReceiveComponent;
  let fixture: ComponentFixture<ImeiReceiveComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ImeiReceiveComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ImeiReceiveComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
