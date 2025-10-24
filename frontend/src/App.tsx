import PromptConsole from './components/PromptConsole';

const App = () => {
  return (
    <div className="app-container">
      <header>
        <h1>Tone Druid — Prompt Console</h1>
        <p>Describe the sound you are chasing and Tone Druid will respond.</p>
      </header>
      <PromptConsole />
    </div>
  );
};

export default App;
