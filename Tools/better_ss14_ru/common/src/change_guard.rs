use std::ops::{Deref, DerefMut};

pub struct ChangeGuard<'a, T: PartialEq> {
    //is never actually None unless being dropped
    value_copy: Option<T>,
    value_ref: &'a mut T,
    changed_val_ref: &'a mut bool,
}

impl<'a, T: PartialEq + Clone> ChangeGuard<'a, T> {
    pub fn new(value: &'a mut T, changed: &'a mut bool) -> Self {
        Self {
            value_copy: Some(value.clone()),
            value_ref: value,
            changed_val_ref: changed,
        }
    }
}

impl<'a, T: PartialEq> Drop for ChangeGuard<'a, T> {
    fn drop(&mut self) {
        // SAFETY: This is safe as this value is never actually None during any access,
        // it's only used to be able to take its value when dropping
        *self.changed_val_ref = *self.changed_val_ref
            || unsafe { self.value_copy.as_ref().unwrap_unchecked() }.ne(&self.value_ref);

        // SAFETY: See above
        *self.value_ref = unsafe { self.value_copy.take().unwrap_unchecked() };
    }
}

impl<'a, T: PartialEq> Deref for ChangeGuard<'a, T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        // SAFETY: This is safe as this value is never actually None during any access,
        // it's only used to be able to take its value when dropping
        unsafe { self.value_copy.as_ref().unwrap_unchecked() }
    }
}

impl<'a, T: PartialEq> DerefMut for ChangeGuard<'a, T> {
    fn deref_mut(&mut self) -> &mut Self::Target {
        // SAFETY: This is safe as this value is never actually None during any access,
        // it's only used to be able to take its value when dropping
        unsafe { self.value_copy.as_mut().unwrap_unchecked() }
    }
}
