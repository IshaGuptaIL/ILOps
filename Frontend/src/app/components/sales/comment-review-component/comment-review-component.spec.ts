import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CommentReviewComponent } from './comment-review-component';

describe('CommentReviewComponent', () => {
  let component: CommentReviewComponent;
  let fixture: ComponentFixture<CommentReviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentReviewComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CommentReviewComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
