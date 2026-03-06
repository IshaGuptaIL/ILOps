import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecieveImeiComponent } from './recieve-imei-component';

describe('RecieveImeiComponent', () => {
  let component: RecieveImeiComponent;
  let fixture: ComponentFixture<RecieveImeiComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecieveImeiComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RecieveImeiComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
