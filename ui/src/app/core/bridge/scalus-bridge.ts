import { InjectionToken } from '@angular/core';

export type Platform = 'Windows' | 'Linux' | 'Mac';

export type TemplateLineEnding = 'Default' | 'Lf' | 'CrLf' | 'Platform';
export type TemplateEncoding = 'Default' | 'Utf8' | 'Utf8Bom' | 'Utf16LeBom' | 'Ansi';

export interface ParserConfig {
  ParserId: string;
  Options?: string[];
  TemplateContent?: string | null;
  TemplateExtension?: string | null;
  LineEnding?: TemplateLineEnding;
  Encoding?: TemplateEncoding;
  PostProcessingExec?: string;
  PostProcessingArgs?: string[];
  /** Host this launch in the user's preferred terminal (SSH/telnet clients). */
  RunInTerminal?: boolean;
}

export interface ApplicationConfig {
  Id: string;
  Name: string;
  Description?: string;
  Platforms: Platform[];
  Protocol: string;
  Parser: ParserConfig;
  Exec: string;
  Args?: string[];
}

export interface ProtocolMapping {
  Protocol: string;
  AppId?: string | null;
}

export interface ScalusConfig {
  Protocols: ProtocolMapping[];
  Applications: ApplicationConfig[];
  /** Global preferred terminal id for terminal-hosted launches (SSH). Omitted/`auto` = detect. */
  PreferredTerminal?: string | null;
}

/** A terminal choice offered for the global "Preferred terminal" setting. */
export interface TerminalOption {
  Id: string;
  Name: string;
  Available: boolean;
}

export type RegistrationScope = 'user' | 'all';

export type RegistrationState = 'registered' | 'conflict' | 'unregistered';

export interface RegistrationStatus {
  Protocol: string;
  State: RegistrationState;
  /** Friendly name of the conflicting application (conflict only). */
  Program?: string | null;
  /** Resolved executable path of the conflicting handler (conflict only). */
  Path?: string | null;
  /** Raw registered command line of the conflicting handler (conflict only). */
  Command?: string | null;
}

export interface ScalusBridge {
  getConfig(): Promise<ScalusConfig>;
  saveConfig(config: ScalusConfig): Promise<{ errors: string[] }>;
  validate(config: ScalusConfig): Promise<string[]>;
  getRegistrations(): Promise<string[]>;
  getRegistrationStatus(): Promise<RegistrationStatus[]>;
  register(protocol: string, scope: RegistrationScope): Promise<void>;
  unregister(protocol: string): Promise<void>;
  getTokens(): Promise<Record<string, string>>;
  getApplicationDescriptions(): Promise<Record<string, string>>;
  getParsers(): Promise<string[]>;
  getTerminals(): Promise<TerminalOption[]>;
  getInfo(): Promise<string>;
  exportToFile(defaultName: string, contents: string): Promise<boolean>;
  importFromFile(): Promise<string | null>;
  getPlatform(): Promise<Platform>;
}

export const SCALUS_BRIDGE = new InjectionToken<ScalusBridge>('SCALUS_BRIDGE');
