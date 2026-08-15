import { expect, test } from '@playwright/test';

test('protects chat, signs in, and signs out', async ({ page }) => {
  await page.goto('/chat');
  await expect(page).toHaveURL(/\/login\?redirectTo=/);
  await expect(page.getByRole('heading', { name: 'Sign in to FlowChat' })).toBeVisible();

  await page.getByLabel('Email or FriendlyUserId').fill('alex.morgan');
  await page.getByLabel('Password').fill('correct-password');
  await page.getByRole('button', { name: 'Sign in' }).click();

  await expect(page).toHaveURL('/chat');
  await expect(page.getByRole('heading', { name: 'Your chat workspace is next.' })).toBeVisible();
  await expect(page.getByText('Signed in as alex.morgan')).toBeVisible();

  await page.getByRole('button', { name: 'Sign out' }).click();
  await expect(page).toHaveURL('/login');
});

test('registers an account and prefills the login', async ({ page }) => {
  await page.goto('/register');
  await page.getByLabel('Email').fill('new.user@example.com');
  await page.getByLabel('FriendlyUserId').fill('New.User');
  await page.getByLabel('First name').fill('New');
  await page.getByLabel('Last name').fill('User');
  await page.getByLabel('Organization').fill('FlowChat');
  await page.getByLabel('Password').fill('password123');
  await page.getByRole('button', { name: 'Create account' }).click();

  await expect(page).toHaveURL('/login');
  await expect(page.getByLabel('Email or FriendlyUserId')).toHaveValue('new.user@example.com');
});

test('confirms an email and retries a temporary failure', async ({ page }) => {
  await page.goto('/email-verification?token=valid');
  await expect(page.getByRole('heading', { name: 'Email confirmed' })).toBeVisible();

  await page.goto('/email-verification?token=retryable');
  await expect(page.getByRole('heading', { name: 'Verification failed' })).toBeVisible();
  await page.getByRole('button', { name: 'Try again' }).click();
  await expect(page.getByRole('heading', { name: 'Email confirmed' })).toBeVisible();
});
