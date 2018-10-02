import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TokenMigrationPageComponent } from './token-migration-page.component';

describe('TokenMigrationPageComponent', () => {
  let component: TokenMigrationPageComponent;
  let fixture: ComponentFixture<TokenMigrationPageComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TokenMigrationPageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TokenMigrationPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
