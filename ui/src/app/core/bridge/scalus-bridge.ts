import { InjectionToken } from '@angular/core';

export type Platform = 'Windows' | 'Linux' | 'Mac';

export interface ParserConfig {
  ParserId: string;
  Options?: string[];
  UseDefaultTemplate?: boolean;
  UseTemplateFile?: string;
  PostProcessingExec?: string;
  PostProcessingArgs?: string[];
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
}

export type RegistrationScope = 'user' | 'all';

export interface ScalusBridge {
  getConfig(): Promise<ScalusConfig>;
  saveConfig(config: ScalusConfig): Promise<{ errors: string[] }>;
  validate(config: ScalusConfig): Promise<string[]>;
  getRegistrations(): Promise<string[]>;
  register(protocol: string, scope: RegistrationScope): Promise<void>;
  unregister(protocol: string): Promise<void>;
  getTokens(): Promise<Record<string, string>>;
  getApplicationDescriptions(): Promise<Record<string, string>>;
  getParsers(): Promise<string[]>;
  getInfo(): Promise<string>;
  exportToFile(defaultName: string, contents: string): Promise<boolean>;
  importFromFile(): Promise<string | null>;
  getPlatform(): Promise<Platform>;
}

export const SCALUS_BRIDGE = new InjectionToken<ScalusBridge>('SCALUS_BRIDGE');
