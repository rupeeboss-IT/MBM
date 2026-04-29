import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MediaKit } from './media-kit';

describe('MediaKit', () => {
  let component: MediaKit;
  let fixture: ComponentFixture<MediaKit>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MediaKit],
    }).compileComponents();

    fixture = TestBed.createComponent(MediaKit);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
