import { expect, test } from 'vitest';
import { roleHomePath } from './roleNavigation';

test.each([['Customer','/dashboard'],['Architect','/architect/dashboard'],['Constructor','/constructor/dashboard'],['Admin','/dashboard']] as const)('%s uses its trusted home route',(role,path)=>expect(roleHomePath(role)).toBe(path));
