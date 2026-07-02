import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { MockBridge } from './core/bridge/mock-bridge';
import { SCALUS_BRIDGE } from './core/bridge/scalus-bridge';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [MockBridge, { provide: SCALUS_BRIDGE, useExisting: MockBridge }]
    }).compileComponents();
  });

  it('renders the four configuration screens in the shell', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Protocols');
    expect(text).toContain('Applications');
    expect(text).toContain('Import / Export');
    expect(text).toContain('About');
  });
});
