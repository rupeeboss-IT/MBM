import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NewsBlog } from './news-blog';

describe('NewsBlog', () => {
  let component: NewsBlog;
  let fixture: ComponentFixture<NewsBlog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NewsBlog],
    }).compileComponents();

    fixture = TestBed.createComponent(NewsBlog);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
