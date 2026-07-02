import { MockBridge } from './mock-bridge';

describe('MockBridge', () => {
  it('is seeded with protocol and application data', async () => {
    const bridge = new MockBridge();
    const config = await bridge.getConfig();
    expect(config.Protocols.map(p => p.Protocol)).toEqual(['rdp', 'ssh', 'telnet']);
    expect(config.Applications.length).toBeGreaterThan(5);
  });

  it('registers and unregisters schemes in memory', async () => {
    const bridge = new MockBridge();
    await bridge.register('telnet', 'user');
    expect(await bridge.getRegistrations()).toContain('telnet');
    await bridge.unregister('telnet');
    expect(await bridge.getRegistrations()).not.toContain('telnet');
  });
});
