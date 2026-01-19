<script lang="ts">
    import { onMount, setContext, type Snippet } from "svelte";
    import { writable } from "svelte/store";
    import ModalProvider from "$lib/components/ModalProvider.svelte";
    
    let { children }: { children: Snippet } = $props();
    
    const colorKey: string = "colorTheme";
    
    interface colorMode {
        name: string,
        value: "light" | "dark" | "auto",
        icon: string
    }
    const colorModes: colorMode[] = [
        { name: "Hell", value: "light", icon: "light_mode"},
        { name: "Dunkel", value: "dark", icon: "dark_mode"},
        { name: "Automatisch", value: "auto", icon: "contrast"}
    ];
    
    let modalProvider: ModalProvider;
    const modalAccessor = writable<ModalProvider>();
    setContext("modalProvider", modalAccessor);
    
    let isDarkMode: boolean = $state(false);
    let currentColor: colorMode = $state(colorModes[2]);
    onMount(() => {
        modalAccessor.set(modalProvider);
        
        const savedColor: string | null = localStorage.getItem(colorKey);
        if (savedColor !== null)
            currentColor = colorModes.find(c => c.value === savedColor) ?? colorModes[2];
    });

    $effect(() => {
        localStorage.setItem(colorKey, currentColor.value);
        if (currentColor.value === "auto")
            isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;
        else
            isDarkMode = currentColor.value === "dark";

        const attrValue = isDarkMode ? "dark" : "light";
        document.getElementsByTagName("html")[0].setAttribute("data-bs-theme", attrValue);
    });
</script>

<ModalProvider bind:this={modalProvider} />
<div class="flex-grow-1 d-flex flex-column my-3">{@render children()}</div>

<footer class="d-flex justify-content-between align-items-center py-3 mt-4 border-top">
    <div class="col-md-4 d-flex align-items-center">
        <a href="/" class="mb-3 me-2 mb-md-0 text-body-secondary text-decoration-none lh-1" aria-label="Bootstrap">
            <svg class="bi" width="30" height="24" aria-hidden="true"><use xlink:href="#bootstrap"></use></svg> </a>
        <span class="mb-3 mb-md-0 text-body-secondary">© 2026 Suiram1701</span>
    </div>
    
    <div class="d-flex flex-column align-items-center">
        <h6>WerwolfDotnet</h6>
        
        <small class="text-muted">
            Erstellt mit
            <a class="link-secondary" href="https://svelte.dev" target="_blank"><span class="fw-semibold">Svelte</span></a>,
            <a class="link-secondary" href="https://getbootstrap.com/" target="_blank"><span class="fw-semibold">Bootstrap</span></a>
            und
            <a class="link-secondary" href="https://learn.microsoft.com/aspnet/core" target="_blank"><span class="fw-semibold">ASP.NET Core</span></a> im Backend
        </small>
    </div>
    
    <ul class="nav col-md-4 justify-content-end list-unstyled d-flex">
        <li class="ms-3">
            <a class="text-body-secondary" href="https://github.com/Suiram1701/WerwolfDotnet" target="_blank" aria-label="GitHub">
                {#if isDarkMode}
                    <img src="/images/github-mark-white.svg" alt="GitHub-Logo" height="42" />
                {:else}
                    <img src="/images/github-mark.svg" alt="GitHub-Logo" height="42" />
                {/if}
            </a>
        </li>
    </ul>
</footer>

<div class="dropdown position-fixed bottom-0 end-0 mb-5 me-3">
    <button class="btn btn-primary py-2 dropdown-toggle d-flex align-items-center" type="button" data-bs-toggle="dropdown">
        <span class="material-symbols-outlined">{currentColor.icon}</span>
    </button>
    <ul class="dropdown-menu dropdown-menu-end shadow">
        {#each colorModes as mode}
            <li>
                <button type="button" class="dropdown-item d-flex justify-content-between align-items-center" onclick={() => currentColor = mode}>
                    <div class="d-flex align-items-center">
                        <span class="material-symbols-outlined me-1">{mode.icon}</span>
                        {mode.name}
                    </div>
                    {#if currentColor.value === mode.value}
                        <span class="material-symbols-outlined">check</span>
                    {/if}
                </button>
            </li>
        {/each}
    </ul>
</div>