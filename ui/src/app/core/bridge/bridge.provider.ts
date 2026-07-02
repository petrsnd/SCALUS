import { Provider } from '@angular/core';
import { MockBridge } from './mock-bridge';
import { PhotinoBridge } from './photino-bridge';
import { SCALUS_BRIDGE, ScalusBridge } from './scalus-bridge';

function hasPhotino(): boolean {
  return typeof window !== 'undefined' && typeof (window as any).external?.sendMessage === 'function';
}

export function provideScalusBridge(): Provider[] {
  return [
    MockBridge,
    PhotinoBridge,
    { provide: SCALUS_BRIDGE, deps: [MockBridge, PhotinoBridge], useFactory: (mock: MockBridge, photino: PhotinoBridge): ScalusBridge => hasPhotino() ? photino : mock }
  ];
}
