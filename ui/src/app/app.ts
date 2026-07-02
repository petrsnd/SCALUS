import { CommonModule } from '@angular/common';
import { Component, Inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { UiBadgeComponent } from './shared/ui/badge.component';
import { UiButtonComponent } from './shared/ui/button.component';
import { UiCardComponent } from './shared/ui/card.component';
import { UiDrawerComponent } from './shared/ui/drawer.component';
import { UiModalComponent } from './shared/ui/modal.component';
import { UiSegmentedControlComponent } from './shared/ui/segmented-control.component';
import { UiSelectComponent } from './shared/ui/select.component';
import { UiToggleComponent } from './shared/ui/toggle.component';
import { ApplicationConfig, Platform, ProtocolMapping, RegistrationScope, SCALUS_BRIDGE, ScalusBridge, ScalusConfig } from './core/bridge/scalus-bridge';
import { cloneConfig, normalizeApplication, normalizeConfig } from './core/bridge/seed-data';

type Tab = 'protocols' | 'applications' | 'io' | 'about';
type EditorMode = 'new' | 'edit';

const BUILT_IN_PROTOCOLS = new Set(['rdp', 'ssh', 'telnet']);
const TOKEN_GROUPS: Record<string, { connection: string[]; safeguard: boolean }> = {
  rdp: { connection: ['%Host%', '%Port%', '%User%', '%Protocol%', '%AlternateShell%', '%Remoteapplicationname%', '%Remoteapplicationprogram%', '%Remoteapplicationcmdline%'], safeguard: true },
  ssh: { connection: ['%Host%', '%Port%', '%User%', '%Protocol%'], safeguard: true },
  telnet: { connection: ['%Host%', '%Port%', '%User%', '%Protocol%'], safeguard: true },
  url: { connection: ['%Host%', '%Port%', '%User%', '%Protocol%', '%Path%', '%Query%', '%Fragment%'], safeguard: false }
};
const ENV_TOKENS = ['%GeneratedFile%', '%OriginalUrl%', '%RelativeUrl%', '%Home%', '%AppData%', '%TempPath%'];
const SAFEGUARD_TOKENS = ['%Token%', '%Vault%', '%TargetUser%', '%TargetHost%', '%TargetPort%', '%Account%', '%Asset%'];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, UiBadgeComponent, UiButtonComponent, UiCardComponent, UiDrawerComponent, UiModalComponent, UiSegmentedControlComponent, UiSelectComponent, UiToggleComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit {
  tab: Tab = 'protocols';
  config: ScalusConfig = { Protocols: [], Applications: [] };
  registrations = new Set<string>();
  scope: RegistrationScope = 'user';
  platform: Platform = 'Windows';
  parsers: string[] = [];
  tokens: Record<string, string> = {};
  info = '';
  message = '';
  editorOpen = false;
  editorMode: EditorMode = 'new';
  editorOriginalId: string | null = null;
  editor: ApplicationConfig | null = null;
  editorDirty = false;
  editorErrors: string[] = [];
  protocolModalOpen = false;
  newProtocol = '';
  protocolError = '';

  nav: { id: Tab; label: string; icon: string }[] = [
    { id: 'protocols', label: 'Protocols', icon: 'eye' },
    { id: 'applications', label: 'Applications', icon: 'grid' },
    { id: 'io', label: 'Import / Export', icon: 'download' },
    { id: 'about', label: 'About', icon: 'info' }
  ];
  scopeOptions = [{ label: 'This user', value: 'user' }, { label: 'All users', value: 'all' }];
  platformOptions: Platform[] = ['Windows', 'Linux', 'Mac'];

  constructor(@Inject(SCALUS_BRIDGE) private bridge: ScalusBridge) {}

  async ngOnInit(): Promise<void> {
    await this.reload();
    this.parsers = await this.bridge.getParsers();
    this.tokens = await this.bridge.getTokens();
    this.platform = await this.bridge.getPlatform();
    this.info = await this.bridge.getInfo();
  }

  async reload(): Promise<void> {
    this.config = await this.bridge.getConfig();
    this.registrations = new Set(await this.bridge.getRegistrations());
  }

  setTab(tab: Tab): void { this.tab = tab; }
  setScope(value: string): void { this.scope = value as RegistrationScope; }

  get registeredCount(): number { return this.config.Protocols.filter(p => this.registrations.has(p.Protocol)).length; }
  get handlerStatus(): string {
    const total = this.config.Protocols.length;
    if (!total) return 'No protocols configured';
    if (!this.registeredCount) return 'No handlers registered';
    if (this.registeredCount === total) return 'All handlers registered';
    return `${this.registeredCount} of ${total} handlers registered`;
  }
  get handlerTone(): 'ok' | 'warn' | 'muted' { return this.registeredCount === 0 ? 'muted' : this.registeredCount === this.config.Protocols.length ? 'ok' : 'warn'; }
  get elevationText(): string { return this.platform === 'Windows' ? 'Requires administrator' : 'Requires sudo'; }
  get versionLine(): string { return (this.info.split('\n')[0] || 'SCALUS 3.0.0').trim(); }

  appById(id?: string | null): ApplicationConfig | undefined { return this.config.Applications.find(app => app.Id === id); }
  appOptionsFor(protocol: ProtocolMapping): { label: string; value: string }[] {
    const family = this.protocolFamily(protocol.Protocol);
    return this.config.Applications
      .filter(app => this.appMatchesFamily(app, family))
      .map(app => ({ label: `${app.Name} Â· ${app.Parser.ParserId.toUpperCase()}`, value: app.Id }));
  }
  protocolFamily(protocol: string): string { return BUILT_IN_PROTOCOLS.has(protocol) ? protocol : 'any'; }
  appMatchesFamily(app: ApplicationConfig, family: string): boolean {
    if (family === 'any') return true;
    if (family === 'telnet') return ['telnet', 'ssh'].includes(app.Parser.ParserId);
    return app.Parser.ParserId === family || app.Protocol === family;
  }
  isBuiltIn(protocol: string): boolean { return BUILT_IN_PROTOCOLS.has(protocol); }
  isRegistered(protocol: string): boolean { return this.registrations.has(protocol); }
  protocolIcon(protocol: string): string { return protocol === 'rdp' ? 'monitor' : protocol === 'ssh' ? 'terminal' : protocol === 'telnet' ? 'window' : 'link'; }
  protocolLine(mapping: ProtocolMapping): string {
    const app = this.appById(mapping.AppId);
    if (!app) return 'Assign an application to enable its handler';
    if (this.isRegistered(mapping.Protocol)) return `${app.Name} is the registered OS handler`;
    return `Turn on Register handler to make ${app.Name} the OS handler`;
  }
  execLeaf(exec: string): string { return (exec || '').replace(/^"([^"]+)".*$/, '$1').split(/[\\/]/).pop()?.split(/\s+/)[0] || exec; }
  appStatus(app: ApplicationConfig): { label: string; tone: 'ok' | 'warn' | 'muted' } {
    if (!app.Platforms.includes(this.platform)) return { label: 'Other platform', tone: 'muted' };
    if (this.config.Protocols.some(p => p.AppId === app.Id)) return { label: 'In use', tone: 'ok' };
    return { label: 'Available', tone: 'muted' };
  }

  async onAppForProtocol(protocol: ProtocolMapping, appId: string | null): Promise<void> {
    if (!appId && this.isRegistered(protocol.Protocol)) await this.toggleRegistration(protocol, false);
    protocol.AppId = appId;
    await this.saveCurrentConfig('Protocol mapping updated.');
  }

  async toggleRegistration(protocol: ProtocolMapping, on: boolean): Promise<void> {
    if (on && !protocol.AppId) return;
    if (on) { await this.bridge.register(protocol.Protocol, this.scope); this.registrations.add(protocol.Protocol); }
    else { await this.bridge.unregister(protocol.Protocol); this.registrations.delete(protocol.Protocol); }
    this.flash(on ? `${protocol.Protocol}:// registered.` : `${protocol.Protocol}:// unregistered.`);
  }

  openProtocolModal(): void { this.newProtocol = ''; this.protocolError = ''; this.protocolModalOpen = true; }
  closeProtocolModal(): void { this.protocolModalOpen = false; }
  async addProtocol(): Promise<void> {
    const scheme = this.newProtocol.trim().toLowerCase().replace(/:\/\/$/, '');
    if (!/^[a-z][a-z0-9+.-]*$/.test(scheme)) { this.protocolError = 'Use a valid URI scheme such as myapp.'; return; }
    if (this.config.Protocols.some(p => p.Protocol === scheme)) { this.protocolError = `${scheme}:// already exists.`; return; }
    this.config.Protocols.push({ Protocol: scheme, AppId: null });
    await this.saveCurrentConfig('Protocol added.');
    this.closeProtocolModal();
  }
  async removeProtocol(protocol: ProtocolMapping): Promise<void> {
    if (this.isBuiltIn(protocol.Protocol)) return;
    if (this.isRegistered(protocol.Protocol)) await this.toggleRegistration(protocol, false);
    this.config.Protocols = this.config.Protocols.filter(p => p !== protocol);
    await this.saveCurrentConfig('Protocol removed.');
  }

  newApplication(): void {
    this.editorMode = 'new';
    this.editorOriginalId = null;
    this.editor = { Id: this.uniqueId('new-app'), Name: '', Description: '', Platforms: [this.platform], Protocol: 'rdp', Parser: { ParserId: 'rdp', Options: [], UseDefaultTemplate: true }, Exec: '', Args: ['%GeneratedFile%'] };
    this.editorOpen = true; this.editorDirty = false; this.editorErrors = [];
  }
  editApplication(app: ApplicationConfig): void {
    this.editorMode = 'edit'; this.editorOriginalId = app.Id; this.editor = JSON.parse(JSON.stringify(app)); this.editorOpen = true; this.editorDirty = false; this.editorErrors = [];
  }
  closeEditor(): void { if (!this.editorDirty || confirm('Discard unsaved changes?')) this.editorOpen = false; }
  markDirty(): void { this.editorDirty = true; }
  parserChanged(parser: string): void {
    if (!this.editor) return;
    this.editor.Parser.ParserId = parser;
    this.editor.Protocol = parser === 'url' ? this.editor.Protocol : parser;
    if (parser === 'rdp') { this.editor.Parser.UseDefaultTemplate = true; this.editor.Args = this.editor.Args?.length ? this.editor.Args : ['%GeneratedFile%']; }
    else { delete this.editor.Parser.UseDefaultTemplate; delete this.editor.Parser.UseTemplateFile; }
    this.markDirty();
  }
  setTemplateMode(mode: 'none' | 'default' | 'file'): void {
    if (!this.editor) return;
    delete this.editor.Parser.UseDefaultTemplate; delete this.editor.Parser.UseTemplateFile;
    if (mode === 'default') this.editor.Parser.UseDefaultTemplate = true;
    if (mode === 'file') this.editor.Parser.UseTemplateFile = this.editor.Parser.UseTemplateFile || '%AppData%\\template.txt';
    this.markDirty();
  }
  templateMode(): 'none' | 'default' | 'file' {
    const parser = this.editor?.Parser;
    if (parser?.UseDefaultTemplate) return 'default';
    if (parser?.UseTemplateFile) return 'file';
    return 'none';
  }
  togglePlatform(platform: Platform, checked: boolean): void {
    if (!this.editor) return;
    const set = new Set(this.editor.Platforms);
    checked ? set.add(platform) : set.delete(platform);
    this.editor.Platforms = Array.from(set);
    this.markDirty();
  }
  setArgsText(value: string): void { if (this.editor) { this.editor.Args = value.split('\n').map(x => x.trim()).filter(Boolean); this.markDirty(); } }
  argsText(): string { return (this.editor?.Args || []).join('\n'); }
  setOptionsText(value: string): void { if (this.editor) { this.editor.Parser.Options = value.split(/\n|,/).map(x => x.trim()).filter(Boolean); this.markDirty(); } }
  optionsText(): string { return (this.editor?.Parser.Options || []).join('\n'); }
  setPostArgsText(value: string): void { if (this.editor) { this.editor.Parser.PostProcessingArgs = value.split('\n').map(x => x.trim()).filter(Boolean); this.markDirty(); } }
  postArgsText(): string { return (this.editor?.Parser.PostProcessingArgs || []).join('\n'); }
  tokenGroups(): { label: string; tokens: string[]; tone: 'brand' | 'warn' | 'muted' }[] {
    const parser = this.editor?.Parser.ParserId || 'url';
    const spec = TOKEN_GROUPS[parser] || TOKEN_GROUPS['url'];
    const env = this.templateMode() === 'none' ? ENV_TOKENS.filter(t => t !== '%GeneratedFile%') : ENV_TOKENS;
    const groups: { label: string; tokens: string[]; tone: 'brand' | 'warn' | 'muted' }[] = [
      { label: `Connection tokens · ${parser}`, tokens: spec.connection, tone: 'brand' },
      { label: 'Generated file & environment', tokens: env, tone: 'muted' }
    ];
    if (spec.safeguard) groups.push({ label: 'Safeguard in-band tokens', tokens: SAFEGUARD_TOKENS, tone: 'warn' });
    return groups;
  }
  insertToken(token: string, target: HTMLTextAreaElement): void {
    const start = target.selectionStart ?? target.value.length;
    const end = target.selectionEnd ?? target.value.length;
    target.value = target.value.slice(0, start) + token + target.value.slice(end);
    target.dispatchEvent(new Event('input'));
    target.focus(); target.selectionStart = target.selectionEnd = start + token.length;
  }
  commandPreview(): string {
    if (!this.editor) return '';
    const sample: Record<string, string> = { '%Host%': 'sps.example.com', '%Port%': '3389', '%User%': 'gwuser\\account~svc-admin%asset~db01%token~a1b2c3', '%GeneratedFile%': 'C:\\Users\\dan\\AppData\\Local\\Temp\\scalus-8f21.rdp', '%OriginalUrl%': 'rdp://â€¦', '%RelativeUrl%': 'full address:s:sps.example.com', '%Token%': 'a1b2c3', '%TargetHost%': 'db01.internal' };
    return `${this.editor.Exec || 'client.exe'} ${(this.editor.Args || []).join(' ')}`.replace(/%[A-Za-z]+%/g, token => sample[token] || token);
  }
  async saveEditor(): Promise<void> {
    if (!this.editor) return;
    const app = JSON.parse(JSON.stringify(this.editor)) as ApplicationConfig;
    app.Id = app.Id.trim() || this.uniqueId(app.Name || 'application');
    app.Name = app.Name.trim();
    this.editorErrors = await this.bridge.validate({ ...this.config, Applications: this.upsertApplication(this.config.Applications, app, this.editorOriginalId) });
    if (this.editorErrors.length) return;
    this.config.Applications = this.upsertApplication(this.config.Applications, app, this.editorOriginalId);
    if (this.editorOriginalId && this.editorOriginalId !== app.Id) {
      this.config.Protocols.forEach(p => { if (p.AppId === this.editorOriginalId) p.AppId = app.Id; });
    }
    await this.saveCurrentConfig('Application saved.');
    this.editorOpen = false;
  }
  upsertApplication(apps: ApplicationConfig[], app: ApplicationConfig, originalId: string | null): ApplicationConfig[] {
    const next = apps.filter(a => a.Id !== (originalId ?? app.Id));
    next.push(app);
    return next.sort((a, b) => a.Name.localeCompare(b.Name));
  }
  async duplicateApplication(app: ApplicationConfig): Promise<void> {
    const copy = JSON.parse(JSON.stringify(app)) as ApplicationConfig;
    copy.Id = this.uniqueId(`${app.Id}-copy`); copy.Name = this.uniqueName(`${app.Name} (copy)`);
    this.config.Applications.push(copy);
    await this.saveCurrentConfig(`Duplicated as ${copy.Name}.`);
  }
  async removeApplication(app: ApplicationConfig): Promise<void> {
    this.config.Protocols.filter(p => p.AppId === app.Id).forEach(p => { p.AppId = null; if (this.isRegistered(p.Protocol)) this.registrations.delete(p.Protocol); });
    this.config.Applications = this.config.Applications.filter(a => a.Id !== app.Id);
    await this.saveCurrentConfig('Application removed.');
  }
  async exportApplication(app: ApplicationConfig): Promise<void> {
    const payload = { schemaVersion: 1, kind: 'application', Applications: [app] };
    await this.bridge.exportToFile(`${app.Id}.scalus-app.json`, JSON.stringify(payload, null, 2));
  }

  async exportAll(): Promise<void> {
    const payload = { schemaVersion: 1, kind: 'full', ...this.config };
    await this.bridge.exportToFile('scalus.json', JSON.stringify(payload, null, 2));
  }
  async importApplication(): Promise<void> {
    const text = await this.bridge.importFromFile(); if (!text) return;
    const raw = JSON.parse(text);
    const appsRaw = raw.kind === 'application' ? (raw.Applications ?? []) : (raw.Applications?.length === 1 ? raw.Applications : []);
    if (!appsRaw.length) { this.flash('No application found in import file.'); return; }
    for (const rawApp of appsRaw) {
      const app = normalizeApplication(rawApp); if (!app) continue;
      app.Id = this.uniqueId(app.Id); app.Name = this.uniqueName(app.Name);
      this.config.Applications.push(app);
    }
    await this.saveCurrentConfig('Application imported.');
    this.tab = 'applications';
  }
  async importReplace(): Promise<void> {
    const text = await this.bridge.importFromFile(); if (!text) return;
    const parsed = normalizeConfig(JSON.parse(text));
    if (!parsed) { this.flash('Import file is not a full SCALUS configuration.'); return; }
    if (!confirm(`Replace the entire configuration with ${parsed.Applications.length} applications and ${parsed.Protocols.length} protocols?`)) return;
    this.config = parsed;
    await this.saveCurrentConfig('Configuration replaced.');
    this.registrations = new Set(await this.bridge.getRegistrations());
  }

  private async saveCurrentConfig(success: string): Promise<void> {
    const result = await this.bridge.saveConfig(cloneConfig(this.config));
    this.editorErrors = result.errors;
    if (!result.errors.length) this.flash(success);
  }
  private uniqueId(seed: string): string {
    const base = (seed || 'application').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'application';
    const ids = new Set(this.config.Applications.map(a => a.Id));
    let id = base, i = 2;
    while (ids.has(id)) id = `${base}-${i++}`;
    return id;
  }
  private uniqueName(seed: string): string {
    const names = new Set(this.config.Applications.map(a => a.Name));
    let name = seed, i = 2;
    while (names.has(name)) name = `${seed} ${i++}`;
    return name;
  }
  private flash(message: string): void { this.message = message; window.setTimeout(() => { if (this.message === message) this.message = ''; }, 2600); }

  iconPath(name: string): string {
    const paths: Record<string, string> = {
      eye: 'M247.31 124.76c-.35-.79-8.82-19.58-27.65-38.41C194.57 61.26 162.88 48 128 48S61.43 61.26 36.34 86.35C17.51 105.18 9 124 8.69 124.76a8 8 0 0 0 0 6.5c.35.79 8.82 19.57 27.65 38.4C61.43 194.74 93.12 208 128 208s66.57-13.26 91.66-38.34c18.83-18.83 27.3-37.61 27.65-38.4a8 8 0 0 0 0-6.5ZM128 168a40 40 0 1 1 40-40 40 40 0 0 1-40 40Z',
      grid: 'M104 40H56a16 16 0 0 0-16 16v48a16 16 0 0 0 16 16h48a16 16 0 0 0 16-16V56a16 16 0 0 0-16-16Zm96 0h-48a16 16 0 0 0-16 16v48a16 16 0 0 0 16 16h48a16 16 0 0 0 16-16V56a16 16 0 0 0-16-16Zm-96 96H56a16 16 0 0 0-16 16v48a16 16 0 0 0 16 16h48a16 16 0 0 0 16-16v-48a16 16 0 0 0-16-16Zm96 0h-48a16 16 0 0 0-16 16v48a16 16 0 0 0 16 16h48a16 16 0 0 0 16-16v-48a16 16 0 0 0-16-16Z',
      download: 'M224 152v56a16 16 0 0 1-16 16H48a16 16 0 0 1-16-16v-56a8 8 0 0 1 16 0v56h160v-56a8 8 0 0 1 16 0Zm-101.66 5.66a8 8 0 0 0 11.32 0l40-40a8 8 0 0 0-11.32-11.32L136 132.69V40a8 8 0 0 0-16 0v92.69l-26.34-26.35a8 8 0 1 0-11.32 11.32Z',
      info: 'M128 24a104 104 0 1 0 104 104A104.11 104.11 0 0 0 128 24Zm-4 48a12 12 0 1 1-12 12 12 12 0 0 1 12-12Zm12 112a16 16 0 0 1-16-16v-40a8 8 0 0 1 0-16 16 16 0 0 1 16 16v40a8 8 0 0 1 0 16Z',
      monitor: 'M208 40H48a24 24 0 0 0-24 24v112a24 24 0 0 0 24 24h64v16H88a8 8 0 0 0 0 16h80a8 8 0 0 0 0-16h-24v-16h64a24 24 0 0 0 24-24V64a24 24 0 0 0-24-24Zm8 136a8 8 0 0 1-8 8H48a8 8 0 0 1-8-8V64a8 8 0 0 1 8-8h160a8 8 0 0 1 8 8Z',
      terminal: 'M216 40H40a16 16 0 0 0-16 16v144a16 16 0 0 0 16 16h176a16 16 0 0 0 16-16V56a16 16 0 0 0-16-16ZM104 158.4l-40 30a8 8 0 0 1-9.6-12.8L86.67 152 54.4 127.9a8 8 0 1 1 9.6-12.8l40 30a8 8 0 0 1 0 12.8ZM192 168h-56a8 8 0 0 1 0-16h56a8 8 0 0 1 0 16Z',
      window: 'M216 40H40a16 16 0 0 0-16 16v144a16 16 0 0 0 16 16h176a16 16 0 0 0 16-16V56a16 16 0 0 0-16-16Zm0 160H40V56h176Z',
      link: 'M137.54 186.36a8 8 0 0 1 0 11.31l-9.94 9.94a56 56 0 0 1-79.22-79.22l24.12-24.12a56 56 0 0 1 76.81-2.28 8 8 0 1 1-10.64 12 40 40 0 0 0-54.85 1.63L59.7 139.72a40 40 0 0 0 56.58 56.58l9.94-9.94a8 8 0 0 1 11.32 0Zm70.08-138a56.08 56.08 0 0 0-79.22 0l-9.94 9.94a8 8 0 0 0 11.32 11.32l9.94-9.94a40 40 0 0 1 56.58 56.58l-24.12 24.12a40 40 0 0 1-54.85 1.63 8 8 0 1 0-10.64 12 56 56 0 0 0 76.81-2.28l24.12-24.12a56.08 56.08 0 0 0 0-79.22Z'
    };
    return paths[name] || paths['link'];
  }
}
