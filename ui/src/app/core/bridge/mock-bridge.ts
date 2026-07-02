import { Injectable } from '@angular/core';
import { cloneConfig, FIELD_DESCRIPTIONS, normalizeConfig, SEED_CONFIG, TOKENS } from './seed-data';
import { Platform, RegistrationScope, RegistrationStatus, ScalusBridge, ScalusConfig } from './scalus-bridge';

@Injectable()
export class MockBridge implements ScalusBridge {
  private config = cloneConfig(SEED_CONFIG);
  private registrations = new Set(['rdp']);
  // Demo seed: something other than this SCALUS owns ssh:// so the conflict state is visible.
  private conflicts = new Map<string, { Program: string; Path: string; Command: string }>([
    ['ssh', { Program: 'PuTTY', Path: 'C:\\Program Files\\PuTTY\\putty.exe', Command: '"C:\\Program Files\\PuTTY\\putty.exe" -ssh %1' }],
  ]);

  async getConfig(): Promise<ScalusConfig> { return cloneConfig(this.config); }

  async saveConfig(config: ScalusConfig): Promise<{ errors: string[] }> {
    const errors = await this.validate(config);
    if (!errors.length) this.config = cloneConfig(config);
    return { errors };
  }

  async validate(config: ScalusConfig): Promise<string[]> {
    const errors: string[] = [];
    const ids = new Set<string>();
    for (const app of config.Applications) {
      if (!app.Id?.trim()) errors.push('Every application needs an Id.');
      if (!app.Name?.trim()) errors.push(`Application ${app.Id || '(new)'} needs a name.`);
      if (!app.Exec?.trim()) errors.push(`${app.Name || app.Id} needs an executable.`);
      if (!app.Parser?.ParserId) errors.push(`${app.Name || app.Id} needs a parser.`);
      if (ids.has(app.Id)) errors.push(`Duplicate application Id: ${app.Id}`);
      ids.add(app.Id);
    }
    for (const protocol of config.Protocols) {
      if (!protocol.Protocol?.trim()) errors.push('Every protocol needs a scheme.');
      if (protocol.AppId && !ids.has(protocol.AppId)) errors.push(`${protocol.Protocol} points at a missing application.`);
    }
    return Array.from(new Set(errors));
  }

  async getRegistrations(): Promise<string[]> { return Array.from(this.registrations).sort(); }

  async getRegistrationStatus(): Promise<RegistrationStatus[]> {
    const schemes = new Set<string>(['rdp', 'ssh']);
    for (const p of this.config.Protocols) { if (p.Protocol) schemes.add(p.Protocol); }
    return Array.from(schemes).sort().map((protocol): RegistrationStatus => {
      if (this.registrations.has(protocol)) return { Protocol: protocol, State: 'registered' };
      const c = this.conflicts.get(protocol);
      if (c) return { Protocol: protocol, State: 'conflict', Program: c.Program, Path: c.Path, Command: c.Command };
      return { Protocol: protocol, State: 'unregistered' };
    });
  }

  async register(protocol: string, _scope: RegistrationScope): Promise<void> { this.registrations.add(protocol); this.conflicts.delete(protocol); }
  async unregister(protocol: string): Promise<void> { this.registrations.delete(protocol); }
  async getTokens(): Promise<Record<string, string>> { return { ...TOKENS }; }
  async getApplicationDescriptions(): Promise<Record<string, string>> { return { ...FIELD_DESCRIPTIONS }; }
  async getParsers(): Promise<string[]> { return ['rdp', 'ssh', 'telnet', 'url']; }
  async getInfo(): Promise<string> { return 'SCALUS 3.0.0\nRuntime: .NET 10 / Photino host\nUI bridge: MockBridge\nConfig: in-memory browser seed'; }
  async getPlatform(): Promise<Platform> { return 'Windows'; }

  async exportToFile(defaultName: string, contents: string): Promise<boolean> {
    const blob = new Blob([contents], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = defaultName;
    anchor.click();
    URL.revokeObjectURL(url);
    return true;
  }

  async importFromFile(): Promise<string | null> {
    if ('showOpenFilePicker' in window) {
      try {
        const [handle] = await (window as any).showOpenFilePicker({ types: [{ description: 'SCALUS JSON', accept: { 'application/json': ['.json', '.scalus-app.json'] } }] });
        const file = await handle.getFile();
        return await file.text();
      } catch { return null; }
    }
    return new Promise((resolve) => {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = '.json,.scalus-app.json,application/json';
      input.onchange = () => {
        const file = input.files?.[0];
        if (!file) { resolve(null); return; }
        const reader = new FileReader();
        reader.onload = () => resolve(String(reader.result ?? ''));
        reader.onerror = () => resolve(null);
        reader.readAsText(file);
      };
      input.click();
    });
  }

  importRawForTest(raw: string): ScalusConfig | null { return normalizeConfig(JSON.parse(raw)); }
}
